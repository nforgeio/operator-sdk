// -----------------------------------------------------------------------------
// FILE:	    RbacRuleGenerator.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;

using Neon.Operator.Attributes;
using Neon.Operator.Rbac;
using Neon.Operator.Webhooks;
using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers
{
    [Generator]
    public class RbacRuleGenerator : ISourceGenerator
    {
        private Dictionary<string, StringBuilder> logs;
        public void Initialize(GeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new RbacRuleReceiver());
        }

        public Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            Assembly assembly = null;
            try
            {
                var runtimeDependencies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
                var targetAssembly = runtimeDependencies
                    .FirstOrDefault(ass => Path.GetFileNameWithoutExtension(ass).Equals(assemblyName.Name, StringComparison.InvariantCultureIgnoreCase));

                if (!String.IsNullOrEmpty(targetAssembly))
                    assembly = Assembly.LoadFrom(targetAssembly);
            }
            catch (Exception)
            {
            }
            return assembly;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var metadataLoadContext   = new MetadataLoadContext(context.Compilation);
            var rbacRules             = ((RbacRuleReceiver)context.SyntaxReceiver)?.RbacAttributesToRegister;
            var classesWithRbac       = ((RbacRuleReceiver)context.SyntaxReceiver)?.ClassesToRegister;
            var controllers           = ((RbacRuleReceiver)context.SyntaxReceiver)?.ControllersToRegister;
            var hasMutatingWebhooks   = ((RbacRuleReceiver)context.SyntaxReceiver)?.HasMutatingWebhooks ?? false;
            var hasValidatingWebhooks = ((RbacRuleReceiver)context.SyntaxReceiver)?.HasValidatingWebhooks ?? false;
            var assemblyAttributes    = ((RbacRuleReceiver)context.SyntaxReceiver)?.AssemblyAttributes;
            var namedTypeSymbols      = context.Compilation.GetNamedTypeSymbols();


            var serviceAccounts     = new List<V1ServiceAccount>();
            var clusterRoles        = new List<V1ClusterRole>();
            var clusterRoleBindings = new List<V1ClusterRoleBinding>();
            var roles               = new List<V1Role>();
            var roleBindings        = new List<V1RoleBinding>();
            var attributes          = new List<IRbacRule>();
            var nameAttribute       = RoslynExtensions.GetAttribute<NameAttribute>(metadataLoadContext, context.Compilation, assemblyAttributes);

            string operatorName        = Regex.Replace(context.Compilation.AssemblyName, @"([a-z])([A-Z])", "$1-$2").ToLower();
            string operatorNamespace   = string.Empty;
            string rbacOutputDirectory = null;

            bool leaderElectionDisabled          = false;
            bool certManagerDisabled             = false;
            bool autoRegisterWebhooks            = false;
            bool manageCustomResourceDefinitions = false;

            logs = new Dictionary<string, StringBuilder>();

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorGenerateRbac", out var generateRbac))
            {
                if (bool.TryParse(generateRbac, out bool generateRbacBool))
                {
                    if (!generateRbacBool)
                    {
                        return;
                    }
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory))
            {
                rbacOutputDirectory = projectDirectory;
            }
            else if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                rbacOutputDirectory = projectDir;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorManifestOutputDir", out var manifestOutDir))
            {
                if (!string.IsNullOrEmpty(manifestOutDir))
                {
                    if (Path.IsPathRooted(manifestOutDir))
                    {
                        rbacOutputDirectory = manifestOutDir;
                    }
                    else
                    {
                        rbacOutputDirectory = Path.Combine(projectDirectory, manifestOutDir);
                    }
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorRbacOutputDir", out var rbacOutDir))
            {
                if (!string.IsNullOrEmpty(rbacOutDir))
                {
                    if (Path.IsPathRooted(rbacOutDir))
                    {
                        rbacOutputDirectory = rbacOutDir;
                    }
                    else
                    {
                        rbacOutputDirectory = Path.Combine(projectDirectory, rbacOutDir);
                    }
                }
            }

            if (string.IsNullOrEmpty(rbacOutputDirectory))
            {
                throw new Exception("RBAC output directory not defined.");
            }

            Directory.CreateDirectory(rbacOutputDirectory);

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorLeaderElectionDisabled", out var leaderElectionString))
            {
                if (bool.TryParse(leaderElectionString, out var leaderElectionBool))
                {
                    leaderElectionDisabled = leaderElectionBool;
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorCertManagerDisabled", out var certManagerString))
            {
                if (bool.TryParse(certManagerString, out var certManagerBool))
                {
                    certManagerDisabled = certManagerBool;
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorName", out var oName))
            {
                if (!string.IsNullOrEmpty(oName))
                {
                    operatorName = oName;
                }
            }

            if (nameAttribute != null)
            {
                operatorName = nameAttribute.Name;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorNamespace", out var operatorNs))
            {
                if (!string.IsNullOrEmpty(operatorNs))
                {
                    operatorNamespace = operatorNs;
                }
            }

            if (string.IsNullOrEmpty(operatorName))
            {
                throw new Exception("OperatorName not defined.");
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorAutoRegisterWebhooks", out var autoRegisterWebhooksString))
            {
                if (bool.TryParse(autoRegisterWebhooksString, out var autoRegisterWebhooksBool))
                {
                    autoRegisterWebhooks = autoRegisterWebhooksBool;
                }
            }

            try
            {
                var serviceAccount = new V1ServiceAccount().Initialize();

                serviceAccount.Metadata.Name = operatorName;

                serviceAccounts.Add(serviceAccount);

                var serviceAccountNs = !string.IsNullOrEmpty(serviceAccount.Metadata.NamespaceProperty) ? serviceAccount.Metadata.NamespaceProperty : "{{ .Release.Namespace }}";

                foreach (var controller in controllers)
                {
                    try
                    {
                        if (manageCustomResourceDefinitions)
                        {
                            continue;
                        }

                        var controllerTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == controller.Identifier.ValueText).SingleOrDefault();
                        var controllerFullyQualifiedName = controllerTypeIdentifier.ToDisplayString(DisplayFormat.NameAndContainingTypesAndNamespaces);
                        var controllerSystemType         = metadataLoadContext.ResolveType(controllerTypeIdentifier);
                        var controllerAttr               = controllerSystemType.GetCustomAttribute<ResourceControllerAttribute>();

                        if (controllerAttr.ManageCustomResourceDefinitions)
                        {
                            manageCustomResourceDefinitions = true;
                        }
                    }
                    catch 
                    {
                        // attribute doesn't exist
                    }
                }

                if (manageCustomResourceDefinitions)
                {
                    attributes.Add(
                        new RbacRule<V1CustomResourceDefinition>(
                            verbs: RbacVerb.All,
                            scope: EntityScope.Cluster));
                }

                if (!leaderElectionDisabled)
                {
                    attributes.Add(
                        new RbacRule<V1Lease>(
                            verbs: RbacVerb.All,
                            scope: EntityScope.Namespaced));
                }

                if (hasMutatingWebhooks && autoRegisterWebhooks)
                {
                    attributes.Add(
                        new RbacRule<V1MutatingWebhookConfiguration>(
                            verbs: RbacVerb.All,
                            scope: EntityScope.Cluster));
                }

                if (hasValidatingWebhooks && autoRegisterWebhooks)
                {
                    attributes.Add(
                        new RbacRule<V1ValidatingWebhookConfiguration>(
                            verbs: RbacVerb.All,
                            scope: EntityScope.Cluster));
                }

                if (!certManagerDisabled)
                {
                    attributes.Add(
                        new RbacRule(
                            apiGroup: "cert-manager.io",
                            resource: "certificates",
                            verbs: RbacVerb.All,
                            scope: EntityScope.Namespaced,
                            @namespace: operatorNamespace));

                    attributes.Add(
                        new RbacRule<V1Secret>(
                            verbs: RbacVerb.Watch,
                            scope: EntityScope.Namespaced,
                            @namespace: operatorNamespace,
                            resourceNames: $"{operatorName}-webhook-tls"));
                }

                foreach (var rbacClass in classesWithRbac)
                {
                    var classTypeIdentifiers = namedTypeSymbols.Where(ntm => ntm.GetFullMetadataName() == rbacClass.GetFullMetadataName());
                    var crSystemType         = metadataLoadContext.ResolveType(classTypeIdentifiers.FirstOrDefault());
                    var crFullyQualifiedName = classTypeIdentifiers.FirstOrDefault().ToDisplayString(DisplayFormat.NameAndContainingTypesAndNamespaces);
                    var rbacRuleAttr         = new List<RbacRuleAttribute>();

                    foreach (var classTypeIdentifier in classTypeIdentifiers)
                    {
                        try
                        {
                            rbacRuleAttr.AddRange(crSystemType.GetCustomAttributes<RbacRuleAttribute>());
                        }
                        catch
                        {
                            // no attributes
                        }
                    }

                    foreach (var attribute in rbacRuleAttr)
                    {
                        attributes.Add(
                            new RbacRule(
                                apiGroup:      attribute.ApiGroup,
                                resource:      attribute.Resource,
                                verbs:         attribute.Verbs,
                                scope:         attribute.Scope,
                                @namespace:    attribute.Namespace,
                                resourceNames: attribute.ResourceNames,
                                subResources:  attribute.SubResources)) ;
                    }

                    var rbacGenericAttr = crSystemType.CustomAttributes?
                        .Where(ca=>ca.AttributeType.IsGenericType)?
                        .Where(ca=>ca.AttributeType.GetGenericTypeDefinition().Equals(typeof(RbacRuleAttribute<>)));

                    foreach (var r in rbacGenericAttr)
                    {
                        var args       = r.NamedArguments;
                        var etype      = r.AttributeType.GenericTypeArguments.FirstOrDefault();
                        var entityType = metadataLoadContext.ResolveType(etype.FullName);
                        var k8sAttr    = entityType.GetCustomAttribute<KubernetesEntityAttribute>();
                        var apiGroup   = k8sAttr.Group;
                        var resource   = k8sAttr.PluralName;

                        var rule = new RbacRule(apiGroup, resource);
                        foreach (var p in r.NamedArguments)
                        {
                            var propertyInfo = typeof(RbacRule).GetProperty(p.MemberInfo.Name);
                            if (propertyInfo != null)
                            {
                                propertyInfo.SetValue(rule, p.TypedValue.Value);
                                continue;
                            }

                            var fieldInfo = typeof(RbacRule).GetField(p.MemberInfo.Name);
                            if (fieldInfo != null)
                            {
                                fieldInfo.SetValue(rule, p.TypedValue.Value);
                                continue;
                            }

                            throw new Exception($"No field or property {p}");
                        }

                        attributes.Add(rule);
                    }
                }

                var clusterRules = attributes
                    .Where(attr => attr.Scope == EntityScope.Cluster)
                    .GroupBy(attr => new
                    {
                        ApiGroups     = attr.ApiGroup,
                        ResourceNames = attr.ResourceNames?.Split(',').Distinct().ToList(),
                        Verbs         = attr.Verbs,
                    })
                    .Select(
                        group => (
                            Verbs:         group.Key.Verbs,
                            ResourceNames: group.Key.ResourceNames,
                            ApiGroups:     group.Select(attr => attr.ApiGroup).ToList(),
                            Resources:     group.Select(attr => attr.Resource).ToList(),
                            SubResources:  group.SelectMany(attr => (attr.SubResources?.Split(',')
                                                                            .Distinct()
                                                                            .Where(x => !string.IsNullOrEmpty(x)).Select(sr => $"{attr.Resource}/{sr}")) ?? Array.Empty<string>())
                            ))
                    .Select(
                        group => new V1PolicyRule
                        {
                            ApiGroups     = group.ApiGroups.Distinct().OrderBy(x => x).ToList(),
                            Resources     = group.Resources.Union(group.SubResources).Distinct().OrderBy(x => x).ToList(),
                            ResourceNames = group.ResourceNames?.Count() > 0 ? group.ResourceNames.OrderBy(x => x).ToList() : null,
                            Verbs         = group.Verbs.ToStrings(),
                        })
                    .Distinct(new PolicyRuleComparer());

                if (clusterRules.Any())
                {
                    var clusterRole = new V1ClusterRole().Initialize();

                    clusterRole.Metadata.Name = operatorName;
                    clusterRole.Rules = clusterRules
                                            .OrderBy(pr => pr.ApiGroups.First())
                                            .ThenBy(pr => pr.Resources.First())
                                            .ThenBy(pr => pr.ResourceNames?.First())
                                            .ToList();

                    clusterRoles.Add(clusterRole);

                    var clusterRoleBinding = new V1ClusterRoleBinding().Initialize();

                    clusterRoleBinding.Metadata.Name = operatorName;
                    clusterRoleBinding.RoleRef = new V1RoleRef(name: clusterRole.Metadata.Name, apiGroup: "rbac.authorization.k8s.io", kind: "ClusterRole");

                    clusterRoleBinding.Subjects = new List<V1Subject>()
                    {
                        new V1Subject(kind: "ServiceAccount", name: operatorName, namespaceProperty: serviceAccountNs)
                    };

                    clusterRoleBindings.Add(clusterRoleBinding);
                }

                var namespaceRules = new Dictionary<string, List<V1PolicyRule>>();

                namespaceRules[operatorNamespace] = attributes.Where(attr =>
                    attr.Scope == EntityScope.Namespaced)
                        .GroupBy(
                            attr => new
                            {
                                ApiGroups     = attr.ApiGroup,
                                ResourceNames = attr.ResourceNames?.Split(',').Distinct().ToList(),
                                Verbs         = attr.Verbs
                            })
                        .Select(
                            group => (
                                Verbs:         group.Key.Verbs,
                                ResourceNames: group.Key.ResourceNames,
                                ApiGroups:     group.Select(attr => attr.ApiGroup).ToList(),
                                Resources:     group.Select(attr => attr.Resource).ToList(),
                                SubResources:  group.SelectMany(attr => (attr.SubResources?.Split(',')
                                                                                .Distinct()
                                                                                .Where(x => !string.IsNullOrEmpty(x)).Select(sr => $"{attr.Resource}/{sr}")) ?? Array.Empty<string>())))
                        .Select(
                            group => new V1PolicyRule
                            {
                                ApiGroups     = group.ApiGroups.Distinct().OrderBy(x => x).ToList(),
                                Resources     = group.Resources.Union(group.SubResources).Distinct().OrderBy(x => x).ToList(),
                                ResourceNames = group.ResourceNames?.Count() > 0 ? group.ResourceNames.OrderBy(x => x).ToList() : null,
                                Verbs         = group.Verbs.ToStrings(),
                            })
                        .Distinct(new PolicyRuleComparer())
                        .ToList();

                if (namespaceRules.Keys.Any())
                {
                    foreach (var @namespace in namespaceRules.Keys)
                    {
                        var namespacedRole = new V1Role().Initialize();

                        namespacedRole.Metadata.Name = operatorName;
                        namespacedRole.Rules         = namespaceRules[@namespace]
                                                            .OrderBy(pr => pr.ApiGroups.First())
                                                            .ThenBy(pr => pr.Resources.First())
                                                            .ThenBy(pr => pr.ResourceNames?.First())
                                                            .ToList();

                        if (!string.IsNullOrEmpty(operatorNamespace))
                        {
                            namespacedRole.Metadata.NamespaceProperty = @namespace;
                        }

                        roles.Add(namespacedRole);

                        var roleBinding = new V1RoleBinding().Initialize();

                        roleBinding.Metadata.Name = operatorName;
                        roleBinding.RoleRef       = new V1RoleRef(name: namespacedRole.Metadata.Name, apiGroup: "rbac.authorization.k8s.io", kind: "Role");
                        roleBinding.Subjects      = new List<V1Subject>()
                        {
                            new V1Subject(kind: "ServiceAccount", name: operatorName, namespaceProperty: serviceAccountNs)
                        };

                        if (!string.IsNullOrEmpty(operatorNamespace))
                        {
                            roleBinding.Metadata.NamespaceProperty = @namespace;
                        }

                        roleBindings.Add(roleBinding);
                    }
                }

                foreach (var sa in serviceAccounts)
                {
                    var saString = KubernetesYaml.Serialize(sa);

                    var saNameString = new StringBuilder();
                    saNameString.Append($"serviceaccount-{sa.Name()}");

                    if (!string.IsNullOrEmpty(sa.Metadata.NamespaceProperty))
                    {
                        saNameString.Append($"-{sa.Namespace()}");
                    }

                    saNameString.Append(Constants.YamlExtension);

                    File.WriteAllText(Path.Combine(rbacOutputDirectory, saNameString.ToString()), saString);
                }

                foreach (var cr in clusterRoles)
                {
                    var crString = KubernetesYaml.Serialize(cr);
                    File.WriteAllText(Path.Combine(rbacOutputDirectory, $"clusterrole-{cr.Name()}.yaml"), crString);
                }

                foreach (var crb in clusterRoleBindings)
                {
                    var crbString = KubernetesYaml.Serialize(crb);
                    File.WriteAllText(Path.Combine(rbacOutputDirectory, $"clusterrolebinding-{crb.Name()}.yaml"), crbString);
                }

                foreach (var r in roles)
                {
                    var rString = KubernetesYaml.Serialize(r);

                    var rNameString = new StringBuilder();
                    rNameString.Append($"role-{r.Name()}");

                    if (!string.IsNullOrEmpty(r.Metadata.NamespaceProperty))
                    {
                        rNameString.Append($"-{r.Namespace()}");
                    }

                    rNameString.Append(Constants.YamlExtension);

                    File.WriteAllText(Path.Combine(rbacOutputDirectory, rNameString.ToString()), rString);
                }

                foreach (var rb in roleBindings)
                {
                    var rbString = KubernetesYaml.Serialize(rb);

                    var rbNameString = new StringBuilder();
                    rbNameString.Append($"rolebinding-{rb.Name()}");

                    if (!string.IsNullOrEmpty(rb.Metadata.NamespaceProperty))
                    {
                        rbNameString.Append($"-{rb.Namespace()}");
                    }

                    rbNameString.Append(Constants.YamlExtension);

                    var outputPath = Path.Combine(rbacOutputDirectory, rbNameString.ToString());

                    if (!File.Exists(outputPath) || File.ReadAllText(outputPath) != rbString)
                    {
                        File.WriteAllText(outputPath, rbString);
                    }
                }
            }
            catch (Exception e)
            {
                Log(context, e);

                throw;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorAnalyzerLoggingEnabled", out var logEnabledString))
            {
                if (bool.TryParse(logEnabledString, out var logEnabledbool))
                {
                    if (!logs.ContainsKey(context.Compilation.AssemblyName))
                    {
                        return;
                    }

                    var log                = logs[context.Compilation.AssemblyName];
                    var logOutputDirectory = projectDirectory;

                    if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorAnalyzerLoggingDir", out var logsOutDir))
                    {
                        if (!string.IsNullOrEmpty(logsOutDir))
                        {
                            if (Path.IsPathRooted(logsOutDir))
                            {
                                logOutputDirectory = Path.Combine(logsOutDir, nameof(RbacRuleGenerator));
                            }
                            else
                            {
                                logOutputDirectory = Path.Combine(projectDirectory, logsOutDir, nameof(RbacRuleGenerator));
                            }
                        }
                    }

                    Directory.CreateDirectory(logOutputDirectory);

                    File.WriteAllText(Path.Combine(logOutputDirectory, $"{context.Compilation.AssemblyName}.log"), log.ToString());
                }
            }
        }

        private void Log(GeneratorExecutionContext context, string message)
        {
            if (!logs.ContainsKey(context.Compilation.AssemblyName))
            {
                logs[context.Compilation.AssemblyName] = new StringBuilder();
            }

            logs[context.Compilation.AssemblyName].AppendLine(message);
        }

        private void Log(GeneratorExecutionContext context, Exception e)
        {
            Log(context, e.Message);
            Log(context, e.StackTrace);
        }
    }

    public sealed class PolicyRuleComparer : IEqualityComparer<V1PolicyRule>
    {
        public bool Equals(V1PolicyRule x, V1PolicyRule y)
        {
            return x.ApiGroups.Except(y.ApiGroups).Any()
                && x.ResourceNames.Except(y.ResourceNames).Any()
                && x.Verbs.Except(y.Verbs).Any()
                && x.Resources.Except(y.Resources).Any();
        }

        public int GetHashCode(V1PolicyRule obj)
        {
            return obj.GetHashCode();
        }
    }
}
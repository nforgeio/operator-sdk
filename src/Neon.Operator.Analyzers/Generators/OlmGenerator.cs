// -----------------------------------------------------------------------------
// FILE:	    OlmGenerator.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Common;
using Neon.K8s.Core;
using Neon.Operator.Analyzers.Receivers;
using Neon.Operator.Attributes;
using Neon.Operator.OperatorLifecycleManager;
using Neon.Operator.Rbac;
using Neon.Operator.Webhooks;
using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers.Generators
{
    [Generator]
    public class OlmGenerator : ISourceGenerator
    {
        internal static readonly DiagnosticDescriptor InstallModeDuplicateError =
            new DiagnosticDescriptor(
                id:                 "NO11001",
                title:              "Each install mode may only be specified once",
                messageFormat:      "'{0}' has been specified multiple times",
                category:           "Operator Lifecycle Manager",
                defaultSeverity:    DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor IconNotFoundError =
            new DiagnosticDescriptor(
                id:                 "NO11002",
                title:              "Icon file not found",
                messageFormat:      "'{0}' could not be found",
                category:           "Operator Lifecycle Manager",
                defaultSeverity:    DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MissingRequiredAttributes =
            new DiagnosticDescriptor(
                id:                 "NO11003",
                title:              "Required attributes not set",
                messageFormat:      "The following attributes are required: [{0}]",
                category:           "Operator Lifecycle Manager",
                defaultSeverity:    DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MissingReviewerError =
            new DiagnosticDescriptor(
                id:                 "NO11004",
                title:              "GitHubUsername Not Specified",
                messageFormat:      "Github username is required for maintainer [{0}]",
                category:           "Operator Lifecycle Manager",
                defaultSeverity:    DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor OwnedAttributeExampleError =
            new DiagnosticDescriptor(
                id:                 "NO11005",
                title:              "Cannot define JSON and YAML",
                messageFormat:      "Cannot define both JSON and YAML examples, please choose just one for [{0}]",
                category:           "Operator Lifecycle Manager",
                defaultSeverity:    DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new OlmReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir);

            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.TargetDir", out var targetDir))
            {
                return;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorOlmOutputDir", out var olmDir))
            {
                if (!string.IsNullOrEmpty(olmDir))
                {
                    targetDir = olmDir;
                }
            }

            targetDir = targetDir.TrimEnd('\\');

            var isTestProject = false;

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.IsTestProject", out var isTestProjectString))
            {
                bool.TryParse(isTestProjectString, out isTestProject);
            }

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.OlmChannels", out var olmChannels);

            var metadataLoadContext    = new MetadataLoadContext(context.Compilation);
            var attributes             = ((OlmReceiver)context.SyntaxReceiver)?.Attributes;
            var namedTypeSymbols       = context.Compilation.GetNamedTypeSymbols();
            var ownedEntities          = GetOwnedEntities(context, metadataLoadContext, attributes);
            var requiredEntities       = GetRequiredEntities(context, metadataLoadContext, attributes);
            var controllers            = ((OlmReceiver)context.SyntaxReceiver)?.ControllersToRegister;
            var hasMutatingWebhooks    = ((OlmReceiver)context.SyntaxReceiver)?.HasMutatingWebhooks ?? false;
            var hasValidatingWebhooks  = ((OlmReceiver)context.SyntaxReceiver)?.HasValidatingWebhooks ?? false;
            var classesWithRbac        = ((OlmReceiver)context.SyntaxReceiver)?.ClassesToRegister;
            var webhooks               = ((OlmReceiver)context.SyntaxReceiver)?.Webhooks;
            var rbacAttributes         = new List<IRbacRule>();
            var leaderElectionDisabled = false;
            var missingRequired        = new List<Type>();
            var createdAt              = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddThh:mm:ss%K");
            var operatorName           = RoslynExtensions.GetAttribute<NameAttribute>(metadataLoadContext, context.Compilation, attributes);
            var displayName            = RoslynExtensions.GetAttribute<DisplayNameAttribute>(metadataLoadContext, context.Compilation, attributes);
            var description            = RoslynExtensions.GetAttribute<DescriptionAttribute>(metadataLoadContext, context.Compilation, attributes);
            var certified              = RoslynExtensions.GetAttribute<CertifiedAttribute>(metadataLoadContext, context.Compilation, attributes)?.Certified ?? false;
            var containerImage         = RoslynExtensions.GetAttribute<ContainerImageAttribute>(metadataLoadContext, context.Compilation, attributes);
            var capabilities           = RoslynExtensions.GetAttribute<CapabilitiesAttribute>(metadataLoadContext, context.Compilation, attributes);
            var repository             = RoslynExtensions.GetAttribute<RepositoryAttribute>(metadataLoadContext, context.Compilation, attributes);
            var maturity               = RoslynExtensions.GetAttribute<MaturityAttribute>(metadataLoadContext, context.Compilation, attributes);
            var provider               = RoslynExtensions.GetAttribute<ProviderAttribute>(metadataLoadContext, context.Compilation, attributes);
            var versionAttribute       = RoslynExtensions.GetAttribute<VersionAttribute>(metadataLoadContext, context.Compilation, attributes);
            var minKubeVersion         = RoslynExtensions.GetAttribute<MinKubeVersionAttribute>(metadataLoadContext, context.Compilation, attributes);
            var defaultChannel         = RoslynExtensions.GetAttribute<DefaultChannelAttribute>(metadataLoadContext, context.Compilation, attributes);
            var webhookPortAttribute   = RoslynExtensions.GetAttribute<WebhookPortAttribute>(metadataLoadContext, context.Compilation, attributes);
            var updateGraph            = RoslynExtensions.GetAttribute<UpdateGraphAttribute>(metadataLoadContext, context.Compilation, attributes);
            var categories             = RoslynExtensions.GetAttributes<CategoryAttribute>(metadataLoadContext, context.Compilation, attributes);
            var keywords               = RoslynExtensions.GetAttributes<KeywordAttribute>(metadataLoadContext, context.Compilation, attributes);
            var maintainers            = RoslynExtensions.GetAttributes<MaintainerAttribute>(metadataLoadContext, context.Compilation, attributes);
            var icons                  = RoslynExtensions.GetAttributes<IconAttribute>(metadataLoadContext, context.Compilation, attributes);
            var installModeAttrs       = RoslynExtensions.GetAttributes<InstallModeAttribute>(metadataLoadContext, context.Compilation, attributes);
            var reviewers              = RoslynExtensions.GetAttributes<ReviewersAttribute>(metadataLoadContext, context.Compilation, attributes);
            var links                  = RoslynExtensions.GetAttributes<LinkAttribute>(metadataLoadContext, context.Compilation, attributes);
            var almExampleJson         = GetOwnedEntityExampleJson(context,metadataLoadContext,attributes);

            var requiredCount = 0;

            requiredCount = AddIfNullOrEmpty<NameAttribute>(operatorName, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<DisplayNameAttribute>(displayName, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<DescriptionAttribute>(description, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<ContainerImageAttribute>(containerImage, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<CapabilitiesAttribute>(capabilities, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<ProviderAttribute>(provider, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<DefaultChannelAttribute>(defaultChannel, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<CategoryAttribute>(categories, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<KeywordAttribute>(keywords, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<MaintainerAttribute>(maintainers, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<IconAttribute>(icons, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<InstallModeAttribute>(installModeAttrs, missingRequired, requiredCount);
            requiredCount = AddIfNullOrEmpty<LinkAttribute>(links, missingRequired, requiredCount);

            if (string.IsNullOrEmpty(olmChannels))
            {
                olmChannels = defaultChannel?.DefaultChannel ?? string.Empty;
            }
            else
            {
                olmChannels = olmChannels.Replace(";", ",");
            }

            if (missingRequired.Count > 0 && missingRequired.Count < requiredCount && !isTestProject)
            {
                var miss = string.Join(", ", missingRequired.Select(x => x.Name));
                context.ReportDiagnostic(
                    Diagnostic.Create(MissingRequiredAttributes,
                    Location.None,
                    string.Join(", ", missingRequired.Select(x => x.Name))));
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorLeaderElectionDisabled", out var leaderElectionString))
            {
                if (bool.TryParse(leaderElectionString, out var leaderElectionBool))
                {
                    leaderElectionDisabled = leaderElectionBool;
                }
            }

            var version = versionAttribute?.Version ?? "";

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorVersion", out var versionString))
            {
                if (!string.IsNullOrEmpty(versionString))
                {
                    version = versionString;
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorImageTag", out var imageTagString))
            {
                if (!string.IsNullOrEmpty(imageTagString))
                {
                    if (containerImage != null)
                    {
                        containerImage.Tag = imageTagString;
                    }
                }
            }

            var webhookPort = webhookPortAttribute?.Port ?? Constants.DefaultWebhookPort;

            if (!leaderElectionDisabled)
            {
                rbacAttributes.Add(
                    new RbacRule<V1Lease>(
                        verbs: RbacVerb.All,
                        scope: EntityScope.Namespaced));
            }

            foreach (var rbacClass in classesWithRbac)
            {
                var classTypeIdentifiers = namedTypeSymbols.Where(ntm => ntm.MetadataName == rbacClass.Identifier.ValueText);
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
                    rbacAttributes.Add(
                        new RbacRule(
                            apiGroup: attribute.ApiGroup,
                            resource: attribute.Resource,
                            verbs: attribute.Verbs,
                            scope: attribute.Scope,
                            resourceNames: attribute.ResourceNames,
                            subResources: attribute.SubResources));
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
                    var rule       = new RbacRule(apiGroup, resource);

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

                    rbacAttributes.Add(rule);
                }
            }

            foreach (var icon in icons)
            {
                var iconPath = icon.Value.Path;

                if (!Path.IsPathRooted(icon.Value.Path))
                {
                    iconPath = Path.Combine(projectDir ?? "", icon.Value.Path);
                }

                if (!File.Exists(iconPath))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(IconNotFoundError,
                        icon.Key.GetLocation(),
                        icon.Value.Path));
                }
            }

            var csv = new V1ClusterServiceVersion().Initialize();

            csv.Metadata             = new k8s.Models.V1ObjectMeta();
            csv.Metadata.Name        = $"{operatorName?.Name}.v{version}";
            csv.Metadata.Annotations = new Dictionary<string, string>
            {
                { "description", description?.ShortDescription },
                { "certified", certified.ToString().ToLower() },
                { "createdAt", createdAt },
                { "capabilities", capabilities?.Capability.ToMemberString()},
                { "containerImage", $"{containerImage?.Repository}:{containerImage?.Tag}" },
                { "repository", repository?.Repository },
                { "categories", string.Join(", ", categories?.SelectMany(c => c.Value.Category.ToStrings()).ToImmutableHashSet().OrderBy(x=>x)) },
            };

            if (!string.IsNullOrEmpty(almExampleJson))
            {
                csv.Metadata.Annotations.Add("alm-examples", almExampleJson);
            }

            csv.Spec                = new V1ClusterServiceVersionSpec();
            csv.Spec.Icon           = icons?.Select(i => i.Value.ToIcon(projectDir)).ToList();
            csv.Spec.Keywords       = keywords?.SelectMany(k => k.Value.GetKeywords()).Distinct().ToList();
            csv.Spec.DisplayName    = displayName?.DisplayName;
            csv.Spec.Description    = description?.FullDescription;
            csv.Spec.Version        = version;
            csv.Spec.Maturity       = maturity?.Maturity;
            csv.Spec.MinKubeVersion = minKubeVersion?.MinKubeVersion;

            csv.Spec.Provider       = new Provider()
            {
                Name = provider?.Name,
                Url  = provider?.Url
            };

            csv.Spec.Maintainers = maintainers?.Select(m => new Maintainer()
            {
                Name  = m.Value.Name,
                Email = m.Value.Email
            }).ToList();

            csv.Spec.Links = links?.Select(l => new Link()
            {
                Name = l.Value.Name,
                Url  = l.Value.Url

            }).ToList();

            if (ownedEntities.Count() > 0)
            {
                csv.Spec.CustomResourceDefinitions       = csv.Spec.CustomResourceDefinitions ?? new CustomResourceDefinitions();
                csv.Spec.CustomResourceDefinitions.Owned = ownedEntities.ToList();
            }

            if (requiredEntities.Count() > 0)
            {
                csv.Spec.CustomResourceDefinitions          = csv.Spec.CustomResourceDefinitions ?? new CustomResourceDefinitions();
                csv.Spec.CustomResourceDefinitions.Required = requiredEntities.ToList();
            }

            var installModes = new List<InstallMode>();

            foreach (var mode in installModeAttrs)
            {
                foreach (var im in mode.Value.Type.GetTypes())
                {
                    if (installModes.Any(i => i.Type == im))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                descriptor:  InstallModeDuplicateError,
                                location:    mode.Key.GetLocation(),
                                messageArgs: [im]));
                    }

                    installModes.Add(new InstallMode()
                    {
                        Supported = mode.Value.Supported,
                        Type      = im
                    });
                }
            }
            csv.Spec.InstallModes = installModes;

            csv.Spec.Install                  = new NamedInstallStrategy();
            csv.Spec.Install.Strategy         = "deployment";
            csv.Spec.Install.Spec             = new StrategyDetailsDeployment();
            csv.Spec.Install.Spec.Permissions =
            [
                new StrategyDeploymentPermission()
                {
                    ServiceAccountName = operatorName?.Name,
                    Rules = rbacAttributes.Where(attr =>
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
                            .ToList()
                }
            ];

            csv.Spec.Install.Spec.ClusterPermissions =
            [
                new StrategyDeploymentPermission()
                {
                    ServiceAccountName = operatorName?.Name,
                    Rules = rbacAttributes
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
                    .Distinct(new PolicyRuleComparer())
                    .ToList()
                }
            ];

            csv.Spec.Install.Spec.Deployments =
            [
                new StrategyDeploymentSpec()
                {
                    Name = operatorName?.Name,
                    Label = new Dictionary<string, string>()
                    {
                        { Constants.Labels.Name, operatorName?.Name },
                        { Constants.Labels.Version, version },
                    },
                    Spec = new V1DeploymentSpec()
                    {
                        Replicas = 1,
                        Selector = new V1LabelSelector()
                        {
                            MatchLabels = new Dictionary<string, string>()
                            {
                                { Constants.Labels.Name, operatorName?.Name },
                            }
                        },
                        Template = new V1PodTemplateSpec()
                        {
                            Metadata = new V1ObjectMeta()
                            {
                                Labels = new Dictionary<string, string>()
                                {
                                   { Constants.Labels.Name, operatorName?.Name },

                                },
                                Annotations = new Dictionary<string, string>()
                                {
                                    { Constants.Annotations.PrometheusPath, "/metrics" },
                                    { Constants.Annotations.PrometheusPort, "9762" },
                                    { Constants.Annotations.PrometheusScheme, "http" },
                                    { Constants.Annotations.PrometheusScrape, "true" },
                                }
                            },
                            Spec = new V1PodSpec()
                            {
                                EnableServiceLinks = false,
                                ServiceAccountName = operatorName?.Name,
                                Containers         =
                                [
                                    new V1Container()
                                    {
                                        Name = operatorName?.Name,
                                        Image = $"{containerImage?.Repository}:{containerImage?.Tag}",
                                        Env =
                                        [
                                            new V1EnvVar()
                                            {
                                                Name  = "LISTEN_PORT",
                                                Value = $"{webhookPort}",
                                            },
                                            new V1EnvVar()
                                            {
                                                Name  = "METRICS_PORT",
                                                Value = "9762",
                                            },
                                            new V1EnvVar()
                                            {
                                                Name      = "KUBERNETES_NAMESPACE",
                                                ValueFrom = new V1EnvVarSource()
                                                {
                                                    FieldRef = new V1ObjectFieldSelector()
                                                    {
                                                        FieldPath = "metadata.namespace"
                                                    }
                                                }
                                            },
                                            new V1EnvVar()
                                            {
                                                Name      = "WATCH_NAMESPACE",
                                                ValueFrom = new V1EnvVarSource()
                                                {
                                                    FieldRef = new V1ObjectFieldSelector()
                                                    {
                                                        FieldPath = "metadata.annotations['olm.targetNamespaces']"
                                                    }
                                                }

                                            },

                                         ],
                                        Ports =
                                        [
                                            new V1ContainerPort()
                                            {
                                                Name          = "http",
                                                ContainerPort = webhookPort,
                                                Protocol      = "TCP"
                                            },
                                            new V1ContainerPort()
                                            {
                                                Name          = "http-metrics",
                                                ContainerPort = 9762,
                                                Protocol      = "TCP"

                                            },
                                        ],
                                        LivenessProbe = new V1Probe()
                                        {
                                            HttpGet = new V1HTTPGetAction()
                                            {
                                                Path = "/healthz",
                                                Port = "http",

                                            },

                                        },
                                        ReadinessProbe = new V1Probe()
                                        {
                                            HttpGet = new V1HTTPGetAction()
                                            {
                                                Path ="/ready",
                                                Port = "http",
                                            }

                                        }
                                    }
                                ]
                            }
                        }
                    },
                }
            ];

            if (webhooks?.Count > 0)
            {
                csv.Spec.WebHookDefinitions = new List<WebHookDescription>();

                foreach (var webhook in webhooks)
                {
                    var webhookNs = webhook.GetNamespace();

                    var webhookSystemType = metadataLoadContext.ResolveType($"{webhookNs}.{webhook.Identifier.ValueText}");
                    var assemblySymbol    = context.Compilation.SourceModule.ReferencedAssemblySymbols.Last();
                    var members           = assemblySymbol.GlobalNamespace.GetNamespaceMembers();
                    var webhookAttribute  = webhookSystemType.GetCustomAttribute<WebhookAttribute>();
                    var webhookRules      = webhookSystemType.GetCustomAttributes<WebhookRuleAttribute>();
                    var typeMembers       = context
                        .Compilation
                        .SourceModule
                        .ReferencedAssemblySymbols
                        .SelectMany(ras => ras.GlobalNamespace.GetNamespaceMembers())
                        .SelectMany(nsm => nsm.GetTypeMembers());

                    var webhookEntityType = webhook
                        .DescendantNodes()?
                        .OfType<BaseListSyntax>()?
                        .Where(dn => dn.DescendantNodes()?.OfType<GenericNameSyntax>()?.Any(gns => gns.Identifier.ValueText.EndsWith("IValidatingWebhook")
                            || gns.Identifier.ValueText.EndsWith("ValidatingWebhookBase") == true
                            || gns.Identifier.ValueText.EndsWith("IMutatingWebhook")
                            || gns.Identifier.ValueText.EndsWith("MutatingWebhookBase")) == true).FirstOrDefault();

                    var webhookTypeIdentifier           = webhookEntityType.DescendantNodes().OfType<IdentifierNameSyntax>().Single();
                    var sdf                             = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                    var webhookTypeIdentifierNamespace  = webhookTypeIdentifier.GetNamespace();
                    var webhookEntityTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == webhookTypeIdentifier.Identifier.ValueText).SingleOrDefault();
                    var webhookEntityFullyQualifiedName = webhookEntityTypeIdentifier.ToDisplayString(sdf);
                    var entitySystemType                = metadataLoadContext.ResolveType(webhookEntityTypeIdentifier);
                    var componentType                   = webhookSystemType.GetCustomAttributes<OperatorComponentAttribute>(true).FirstOrDefault();

                    csv.Spec.WebHookDefinitions.Add
                        (
                            new WebHookDescription()
                            {
                                Type                    = ToWebhookAdmissionType(componentType.ComponentType),
                                GenerateName            = webhookAttribute.Name,
                                AdmissionReviewVersions = webhookAttribute.AdmissionReviewVersions.ToList(),
                                ContainerPort           = webhookPort,
                                TargetPort              = webhookPort,
                                DeploymentName          = operatorName?.Name,
                                FailurePolicy           = webhookAttribute.FailurePolicy.ToMemberString(),
                                SideEffects             = webhookAttribute.SideEffects.ToMemberString(),

                                Rules = webhookRules.Select(r => new V1RuleWithOperations()
                                {
                                    ApiGroups   = r.ApiGroups.ToList(),
                                    ApiVersions = r.ApiVersions.ToList(),
                                    Operations  = r.Operations.ToList(),
                                    Resources   = r.Resources.ToList(),
                                }).ToList(),

                                WebHookPath = CreateEndpoint(entitySystemType, webhookSystemType, ToWebhookAdmissionType(componentType.ComponentType)),
                            }
                        );
                }
            }

            var sb = new StringBuilder();

            sb.AppendLine(Constants.AutoGeneratedYamlHeader);
            sb.AppendLine(KubernetesHelper.YamlSerialize(csv));

            var outputString  = sb.ToString();
            var outputBaseDir = Path.Combine(targetDir, "OperatorLifecycleManager");
            var versionDir    = Path.Combine(outputBaseDir, version);
            var manifestDir   = Path.Combine(versionDir, "manifests");
            var metadataDir   = Path.Combine(versionDir, "metadata");

            Directory.CreateDirectory(outputBaseDir);
            Directory.CreateDirectory(versionDir);
            Directory.CreateDirectory(manifestDir);
            Directory.CreateDirectory(metadataDir);

            var csvPath = Path.Combine(manifestDir, $"{operatorName?.Name.ToLower()}.clusterserviceversion{Constants.YamlExtension}");

            AnalyzerHelper.WriteFileWhenDifferent(csvPath, outputString);

            if (maintainers?.Any(m => m.Value.Reviewer) == true || updateGraph != null)
            {
                var ci = new Ci();

                if(updateGraph != null)
                {
                    ci.UpdateGraph = updateGraph.UpdateGraph;
                }

                if (maintainers?.Any(m => m.Value.Reviewer) == true)
                {
                    ci.AddReviewers = true;

                    // check that github is set for all reviewers
                    if(maintainers.Any(m => m.Value.Reviewer && string.IsNullOrEmpty(m.Value.GitHub)))
                    {
                        foreach (var maintainer in maintainers.Where(m => m.Value.Reviewer && string.IsNullOrEmpty(m.Value.GitHub)))
                        {
                            context.ReportDiagnostic(
                                diagnostic:  Diagnostic.Create(
                                descriptor:  MissingReviewerError,
                                location:    maintainer.Key.GetLocation(),
                                messageArgs: [maintainer.Value.Name]));
                        }
                    }

                    ci.Reviewers.AddRange(maintainers.Where(m => m.Value.Reviewer).Select(m => m.Value.GitHub));
                }

                if (reviewers?.Any() == true)
                {
                    ci.AddReviewers = true;
                    ci.Reviewers.AddRange(reviewers.SelectMany(r=> r.Value.GetReviewers()).Distinct());
                }

                var outputStringCi  = KubernetesHelper.YamlSerialize(ci);
                var ciPath          = Path.Combine(outputBaseDir,"ci.yaml");

                AnalyzerHelper.WriteFileWhenDifferent(ciPath, outputStringCi);
            }

            var annotations = $@"annotations:
  operators.operatorframework.io.bundle.mediatype.v1: ""registry+v1""
  operators.operatorframework.io.bundle.manifests.v1: ""manifests/""
  operators.operatorframework.io.bundle.metadata.v1: ""metadata/""
  operators.operatorframework.io.bundle.package.v1: ""{operatorName?.Name.ToLower()}""
  operators.operatorframework.io.bundle.channels.v1: ""{olmChannels}""
  operators.operatorframework.io.bundle.channel.default.v1: ""{defaultChannel?.DefaultChannel}""
";
            var annotationsPath = Path.Combine(metadataDir, "annotations.yaml");

            AnalyzerHelper.WriteFileWhenDifferent(annotationsPath, annotations);

            var dockerFile = $@"FROM scratch

LABEL operators.operatorframework.io.bundle.mediatype.v1=registry+v1
LABEL operators.operatorframework.io.bundle.manifests.v1=manifests/
LABEL operators.operatorframework.io.bundle.metadata.v1=metadata/
LABEL operators.operatorframework.io.bundle.package.v1={operatorName?.Name.ToLower()}
LABEL operators.operatorframework.io.bundle.channels.v1={olmChannels}
LABEL operators.operatorframework.io.bundle.channel.default.v1={defaultChannel?.DefaultChannel}

ADD ./manifests/*.yaml /manifests/
ADD ./metadata/annotations.yaml /metadata/annotations.yaml
";
            var dockerfilePath = Path.Combine(versionDir, "Dockerfile");

            AnalyzerHelper.WriteFileWhenDifferent(dockerfilePath, dockerFile);
        }
        public static WebhookAdmissionType ToWebhookAdmissionType(OperatorComponentType componentType) => componentType switch
        {
            OperatorComponentType.ValidationWebhook => WebhookAdmissionType.ValidatingAdmissionWebhook,
            OperatorComponentType.MutationWebhook => WebhookAdmissionType.MutatingAdmissionWebhook,_ => throw new ArgumentException()
        };

        private int AddIfNullOrEmpty<T>(object arg, List<Type> types, int requiredCount)
        {
            if (arg == null)
            {
                types.Add(typeof(T));
            }
            else if (typeof(IDictionary).IsAssignableFrom(arg.GetType()))
            {
                if (((IDictionary)arg).Keys.Count == 0)
                {
                    types.Add(typeof(T));
                }
            }
            else if (typeof(IEnumerable).IsAssignableFrom(arg.GetType()))
            {
                if (((IEnumerable<object>)arg).IsEmpty())
                {
                    types.Add(typeof(T));
                }
            }

            requiredCount += 1;

            return requiredCount;
        }

        public string GetOwnedEntityExampleJson(
            GeneratorExecutionContext context,
            MetadataLoadContext       metadataLoadContext,
            List<AttributeSyntax>     attributes)
        {
            try
            {
                var results            = new List<CrdDescription>();
                var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
                var ownedEntities      = assemblyAttributes
                    .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith($"{typeof(OwnedEntityAttribute).Namespace}.OwnedEntity"));

                var jsonStrings = new List<string>();

                foreach (var entity in ownedEntities)
                {
                    var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                    var entityType = metadataLoadContext.ResolveType(etype);
                    var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                    var ownedAttribute = attributes.Where(a => a.Name is GenericNameSyntax)
                        .Where(a => ((GenericNameSyntax)a.Name).Identifier.ValueText == "OwnedEntity"
                          && ((IdentifierNameSyntax)((GenericNameSyntax)a.Name).TypeArgumentList.Arguments.First()).Identifier.ValueText == entityType.Name)
                        .First();

                    var exampleJson = ownedAttribute
                        .ArgumentList
                        .Arguments
                        .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.ExampleJson))
                        .FirstOrDefault()
                        ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var exampleYaml = ownedAttribute
                        .ArgumentList
                        .Arguments
                        .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.ExampleYaml))
                        .FirstOrDefault() ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    if (!string.IsNullOrEmpty(exampleJson) && !string.IsNullOrEmpty(exampleYaml))
                    {
                        context.ReportDiagnostic(
                            diagnostic: Diagnostic.Create(
                                descriptor: OwnedAttributeExampleError,
                                location: ownedAttribute.GetLocation(),
                                messageArgs: [entityType.Name]));
                    }

                    if (!string.IsNullOrEmpty(exampleJson))
                    {
                        jsonStrings.Add(exampleJson);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(exampleYaml))
                    {
                        var obj = KubernetesHelper.YamlDeserialize<dynamic>(exampleYaml, stringTypeDeserialization: false);

                        jsonStrings.Add(KubernetesHelper.JsonSerialize(obj));
                        continue;
                    }

                    dynamic defaultEntity = entityType.GetDefault();

                    defaultEntity.ApiVersion = $"{metadata.Group}/{metadata.ApiVersion}";
                    defaultEntity.Kind       = metadata.Kind;
                    defaultEntity.Metadata   = new V1ObjectMeta()
                    {
                        Name              = $"my-{metadata.Kind.ToLower()}",
                        NamespaceProperty = "default"
                    };

                    if (defaultEntity != null)
                    {
                        jsonStrings.Add(KubernetesHelper.JsonSerialize(defaultEntity));
                    }
                }

                if (jsonStrings.IsEmpty())
                {
                    return null;
                }

                var sb = new StringBuilder();

                sb.Append("[");
                sb.Append(string.Join(",", jsonStrings));
                sb.Append("]");

                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        public IEnumerable<CrdDescription> GetOwnedEntities(GeneratorExecutionContext context, MetadataLoadContext metadataLoadContext, List<AttributeSyntax> attributes)
        {
            try
            {
                var results            = new List<CrdDescription>();
                var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
                var ownedEntities      = assemblyAttributes.Where(a => a.AttributeClass.GetFullMetadataName().StartsWith($"{typeof(OwnedEntityAttribute).Namespace}.OwnedEntity"));

                foreach (var entity in ownedEntities)
                {
                    var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                    var entityType = metadataLoadContext.ResolveType(etype);
                    var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                    var ownedAttribute = attributes.Where(a => a.Name is GenericNameSyntax)
                        .Where(a => ((GenericNameSyntax)a.Name).Identifier.ValueText == "OwnedEntity" &&
                            ((IdentifierNameSyntax)((GenericNameSyntax)a.Name).TypeArgumentList.Arguments.First()).Identifier.ValueText == entityType.Name);

                    var description = ownedAttribute
                        .First().ArgumentList.Arguments
                            .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.Description))
                            .FirstOrDefault()?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var displayName = ownedAttribute
                        .First().ArgumentList.Arguments
                            .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.DisplayName))
                            .FirstOrDefault()?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var dependents = entityType.CustomAttributes
                        .Where(a => a.AttributeType.GetGenericTypeDefinition().Equals(typeof(DependentResourceAttribute<>)))
                        .Select(a => a.AttributeType.GenericTypeArguments.First())
                        .ToList();

                    var crdDescription = new CrdDescription()
                    {
                        Name    = $"{metadata.PluralName}.{metadata.Group}",
                        Version = metadata.ApiVersion,
                        Kind    = metadata.Kind,
                    };

                    if (!string.IsNullOrEmpty(description))
                    {
                        crdDescription.Description = description;
                    }

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        crdDescription.DisplayName = displayName;
                    }

                    if (dependents?.Count > 0)
                    {
                        crdDescription.Resources = dependents?.Select(d => new ApiResourceReference()
                        {
                            Kind    = d.GetCustomAttribute<KubernetesEntityAttribute>().Kind,
                            Version = d.GetCustomAttribute<KubernetesEntityAttribute>().ApiVersion,
                            Name    = d.GetCustomAttribute<KubernetesEntityAttribute>().PluralName
                        }).ToList();
                    }
                    
                    results.Add(crdDescription);
                }

                return results;
            }
            catch
            {
                return Enumerable.Empty<CrdDescription>();
            }

        }

        public IEnumerable<CrdDescription> GetRequiredEntities(GeneratorExecutionContext context, MetadataLoadContext metadataLoadContext, List<AttributeSyntax> attributes)
        {
            try
            {
                var results            = new List<CrdDescription>();
                var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
                var requiredEntities   = assemblyAttributes
                    .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith($"{typeof(RequiredEntityAttribute).Namespace}.RequiredEntity"));

                foreach (var entity in requiredEntities)
                {
                    var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                    var entityType = metadataLoadContext.ResolveType(etype);
                    var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                    var requiredAttribute = attributes.Where(a => a.Name is GenericNameSyntax)
                        .Where(a => ((GenericNameSyntax)a.Name).Identifier.ValueText == "RequiredEntity" &&
                            ((IdentifierNameSyntax)((GenericNameSyntax)a.Name).TypeArgumentList.Arguments.First()).Identifier.ValueText == entityType.Name);

                    var description = requiredAttribute
                        .First().ArgumentList.Arguments
                            .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(RequiredEntityAttribute.Description))
                            .FirstOrDefault()?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var displayName = requiredAttribute
                        .First().ArgumentList.Arguments
                           .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(RequiredEntityAttribute.DisplayName))
                            .FirstOrDefault()?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var dependents = entityType.CustomAttributes
                        .Where(a => a.AttributeType.GetGenericTypeDefinition().Equals(typeof(DependentResourceAttribute<>)))
                        .Select(a => a.AttributeType.GenericTypeArguments.First())
                        .ToList();

                    var crdDescription = new CrdDescription()
                    {
                        Name    = $"{metadata.PluralName}.{metadata.Group}",
                        Version = metadata.ApiVersion,
                        Kind    = metadata.Kind
                    };

                    if (!string.IsNullOrEmpty(description))
                    {
                        crdDescription.Description = description;
                    }

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        crdDescription.DisplayName = displayName;
                    }

                    crdDescription.Resources = dependents?.Select(d => new ApiResourceReference()
                    {
                        Kind    = d.GetCustomAttribute<KubernetesEntityAttribute>().Kind,
                        Version = d.GetCustomAttribute<KubernetesEntityAttribute>().ApiVersion,
                        Name    = d.GetCustomAttribute<KubernetesEntityAttribute>().PluralName
                    }).ToList();

                    results.Add(crdDescription);
                }

                return results;
            }
            catch
            {
                return Enumerable.Empty<CrdDescription>();
            }
        }

        private string CreateEndpoint(Type entityType, Type webhookImplementation, WebhookAdmissionType webhookAdmissionType)
        {
            var metadata = entityType.GetCustomAttribute<KubernetesEntityAttribute>();
            var builder  = new StringBuilder();

            if (!string.IsNullOrEmpty(metadata.Group))
            {
                builder.Append($"/{metadata.Group}");
            }

            if (!string.IsNullOrEmpty(metadata.ApiVersion))
            {
                builder.Append($"/{metadata.ApiVersion}");
            }

            if (!string.IsNullOrEmpty(metadata.PluralName))
            {
                builder.Append($"/{metadata.PluralName}");
            }

            builder.Append($"/{webhookImplementation.Name}");

            switch (webhookAdmissionType)
            {
                case WebhookAdmissionType.MutatingAdmissionWebhook:
                    builder.Append("/mutate");
                    break;

                case WebhookAdmissionType.ValidatingAdmissionWebhook:
                    builder.Append("/validate");
                    break;
            }

            return builder.ToString().ToLowerInvariant();
        }
    }

    public static class OlmExtensions
    {
        public static T GetCustomAttribute<T>(
            this AttributeSyntax    attributeData,
            MetadataLoadContext     metadataLoadContext,
            Compilation             compilation)
        {
            if (attributeData == null)
            {
                return default(T);
            }

            T attribute;

            // Check for constructor arguments
            if (attributeData.ArgumentList.Arguments.Any(a => a.NameEquals == null) &&
                typeof(T).GetConstructors().Any(c => c.GetParameters().Length > 0))
            {
                // create instance with constructor args
                var actualArgs = attributeData.ArgumentList.Arguments
                    .Where(a => a.NameEquals == null)
                    .Select(a => a.Expression.GetExpressionValue(metadataLoadContext)).ToArray();

                attribute = (T)Activator.CreateInstance(typeof(T), actualArgs);
            }
            else
            {
                attribute = (T)Activator.CreateInstance(typeof(T));
            }

            // check and et named arguments
            foreach (var p in attributeData.ArgumentList.Arguments.Where(a => a.NameEquals != null))
            {
                var propertyName = p.NameEquals.Name.Identifier.ValueText;

                object value = null;
                //if (p.Expression is BinaryExpressionSyntax
                //    && ((BinaryExpressionSyntax)p.Expression).Kind() == SyntaxKind.BitwiseOrExpression)
                //{
                //    value = ((BinaryExpressionSyntax)p.Expression).GetEnumValue(metadataLoadContext);
                //}
                //else
                //{
                //}
                value = p.Expression.GetExpressionValue(metadataLoadContext);

                var propertyInfo = typeof(T).GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(attribute, value);
                    continue;
                }

                var fieldInfo = typeof(T).GetField(propertyName);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(attribute, value);
                    continue;
                }

                throw new Exception($"No field or property {p}");
            }

            return attribute;
        }
    }
}

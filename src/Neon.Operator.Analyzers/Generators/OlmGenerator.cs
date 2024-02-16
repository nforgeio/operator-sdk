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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Operator.Analyzers.Receivers;
using Neon.Operator.Attributes;
using Neon.Operator.OperatorLifecycleManager;
using Neon.Operator.Rbac;
using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers.Generators
{
    [Generator]
    public class OlmGenerator : ISourceGenerator
    {
        private GeneratorExecutionContext context;
        private MetadataLoadContext metadataLoadContext;
        private List<AttributeSyntax> attributes;

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new OlmReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.TargetDir", out var targetDir))
            {
                return;
            }

            this.context = context;
            metadataLoadContext    = new MetadataLoadContext(context.Compilation);
            attributes             = ((OlmReceiver)context.SyntaxReceiver)?.Attributes;
            var namedTypeSymbols   = context.Compilation.GetNamedTypeSymbols();
            var ownedEntities      = GetOwnedEntities();
            var requiredEntities   = GetRequiredEntities();

            var AssemblyAttributes = new List<IRbacRule>();
            var controllers           = ((RbacRuleReceiver)context.SyntaxReceiver)?.ControllersToRegister;
            var hasMutatingWebhooks   = ((RbacRuleReceiver)context.SyntaxReceiver)?.HasMutatingWebhooks ?? false;
            var hasValidatingWebhooks = ((RbacRuleReceiver)context.SyntaxReceiver)?.HasValidatingWebhooks ?? false;
            var classesWithRbac       = ((RbacRuleReceiver)context.SyntaxReceiver)?.ClassesToRegister;


            bool manageCustomResourceDefinitions = false;
            bool leaderElectionDisabled          = false;
            bool autoRegisterWebhooks            = false;
            bool certManagerDisabled             = false;





            var operatorName     = GetAttribute<NameAttribute>(metadataLoadContext, context.Compilation);
            var displayName      = GetAttribute<DisplayNameAttribute>(metadataLoadContext, context.Compilation);
            var description      = GetAttribute<DescriptionAttribute>(metadataLoadContext, context.Compilation);
            var certified        = GetAttribute<CertifiedAttribute>(metadataLoadContext, context.Compilation)?.Certified ?? false;
            var createdData      = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddThh:mm:ss%K");
            var containerImage   = GetAttribute<ContainerImageAttribute>(metadataLoadContext, context.Compilation);
            var categories       = GetAttributes<CategoryAttribute>(metadataLoadContext, context.Compilation);
            var capabilities     = GetAttribute<CapabilitiesAttribute>(metadataLoadContext, context.Compilation);
            var repository       = GetAttribute<RepositoryAttribute>(metadataLoadContext, context.Compilation);
            var icons            = GetAttributes<IconAttribute>(metadataLoadContext, context.Compilation);
            var keywords         = GetAttributes<KeywordAttribute>(metadataLoadContext, context.Compilation);
            var maintainers      = GetAttributes<MaintainerAttribute>(metadataLoadContext, context.Compilation);
            var provider         = GetAttribute<ProviderAttribute>(metadataLoadContext, context.Compilation);
            var version          = GetAttribute<VersionAttribute>(metadataLoadContext, context.Compilation);


            //if (operatorName == null
            //    || displayName == null)
            //{
            //    return;
            //}

            var csv = new V1ClusterServiceVersion().Initialize();

            csv.Metadata = new k8s.Models.V1ObjectMeta();
            csv.Metadata.Name = $"{operatorName.Name}.v{version.Version}";
            csv.Metadata.Annotations = new Dictionary<string, string>
            {
                { "description", description.ShortDescription },
                { "certified", certified.ToString().ToLower() },
                { "createdAt", createdData },
                { "capabilities", capabilities.Capability.ToString()},
                { "containerImage", $"{containerImage.Repository}:{containerImage.Tag}" },
                { "repository", repository.Repository },
                { "categories", string.Join(", ", categories.SelectMany(c => c.Category.ToStrings()).ToImmutableHashSet()) },
            };


            csv.Spec = new V1ClusterServiceVersionSpec();
            //csv.Spec.Icon = icons?.Select(i => i.ToIcon()).ToList();
            csv.Spec.Keywords = keywords?.SelectMany(k => k.GetKeywords()).Distinct().ToList();
            csv.Spec.DisplayName = displayName.DisplayName;
            csv.Spec.Description = description.FullDescription;
            csv.Spec.Version = version.Version;
            csv.Spec.Provider = new Provider()
            {
                Name = provider.Name,
                Url = provider.Url
            };
            csv.Spec.Maintainers = maintainers?.Select(m => new Maintainer()
            {
                Name = m.Name,
                Email = m.Email
            }).ToList();
            csv.Spec.CustomResourceDefinitions = new CustomResourceDefinitions()
            {
                Owned = ownedEntities,
                Required = requiredEntities
            };

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorLeaderElectionDisabled", out var leaderElectionString))
            {
                if (bool.TryParse(leaderElectionString, out var leaderElectionBool))
                {
                    leaderElectionDisabled = leaderElectionBool;
                }
            }

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
                AssemblyAttributes.Add(
                    new RbacRule<V1CustomResourceDefinition>(
                        verbs: RbacVerb.All,
                        scope: EntityScope.Cluster));
            }
            else
            {
                AssemblyAttributes.Add(
                    new RbacRule<V1CustomResourceDefinition>(
                        verbs: RbacVerb.Get | RbacVerb.List | RbacVerb.Watch,
                        scope: EntityScope.Cluster));
            }

            if (!leaderElectionDisabled)
            {
                AssemblyAttributes.Add(
                    new RbacRule<V1Lease>(
                        verbs: RbacVerb.All,
                        scope: EntityScope.Cluster));
            }

            if (hasMutatingWebhooks && autoRegisterWebhooks)
            {
                AssemblyAttributes.Add(
                    new RbacRule<V1MutatingWebhookConfiguration>(
                        verbs: RbacVerb.All,
                        scope: EntityScope.Cluster));
            }

            if (hasValidatingWebhooks && autoRegisterWebhooks)
            {
                AssemblyAttributes.Add(
                    new RbacRule<V1ValidatingWebhookConfiguration>(
                        verbs: RbacVerb.All,
                        scope: EntityScope.Cluster));
            }

            if (!certManagerDisabled)
            {
                AssemblyAttributes.Add(
                    new RbacRule(
                        apiGroup: "cert-manager.io",
                        resource: "certificates",
                        verbs: RbacVerb.All,
                        scope: EntityScope.Namespaced));

                AssemblyAttributes.Add(
                    new RbacRule<V1Secret>(
                        verbs: RbacVerb.Watch,
                        scope: EntityScope.Namespaced,
                        resourceNames: $"{operatorName}-webhook-tls"));
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
                    AssemblyAttributes.Add(
                        new RbacRule(
                            apiGroup: attribute.ApiGroup,
                            resource: attribute.Resource,
                            verbs: attribute.Verbs,
                            scope: attribute.Scope,
                            @namespace: attribute.Namespace,
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

                    AssemblyAttributes.Add(rule);
                }
            }

            var outputString = KubernetesYaml.Serialize(csv);
            var outputPath = Path.Combine(targetDir, "clusterserviceversion.yaml");
            File.WriteAllText(outputPath, outputString);
        }

        public List<CrdDescription> GetOwnedEntities()
        {
            var results = new List<CrdDescription>();

            var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
            var ownedEntities = assemblyAttributes
                .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith($"{typeof(OwnedEntityAttribute).Namespace}.OwnedEntity"));

            var a = attributes.First();
            var ns = a.GetNamespace();
            var name = a.Name.ToFullString();
            var aType = metadataLoadContext.ResolveType(name);
            var tName = a.TryGetInferredMemberName();

            foreach (var entity in ownedEntities)
            {
                var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                var entityType = metadataLoadContext.ResolveType(etype);
                var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                var ownedAttribute = attributes.Where(a => a.Name is GenericNameSyntax)
                    .Where(a => ((IdentifierNameSyntax)((GenericNameSyntax)a.Name).TypeArgumentList.Arguments.First()).Identifier.ValueText == entityType.Name);

                var description = ownedAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.Description))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                var displayName = ownedAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.DisplayName))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                var dependents = entityType.CustomAttributes
                    .Where(a => a.AttributeType.GetGenericTypeDefinition().Equals(typeof(DependentResourceAttribute<>)))
                    .Select(a => a.AttributeType.GenericTypeArguments.First())
                    .ToList();

                var crdDescription = new CrdDescription()
                {
                    Name        = $"{metadata.PluralName}.{metadata.Group}",
                    Version     = metadata.ApiVersion,
                    Kind        = metadata.Kind,
                    DisplayName = displayName,
                    Description = description
                };

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

        public List<CrdDescription> GetRequiredEntities()
        {
            var results = new List<CrdDescription>();

            var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
            var requiredEntities = assemblyAttributes
                .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith($"{typeof(RequiredEntityAttribute).Namespace}.RequiredEntity"));


            var a = attributes.First();
            var ns = a.GetNamespace();
            var name = a.Name.ToFullString();
            var aType = metadataLoadContext.ResolveType(name);
            var tName = a.TryGetInferredMemberName();

            foreach (var entity in requiredEntities)
            {
                var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                var entityType = metadataLoadContext.ResolveType(etype);
                var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                var requiredAttribute = attributes.Where(a => a.Name is GenericNameSyntax)
                    .Where(a => ((IdentifierNameSyntax)((GenericNameSyntax)a.Name).TypeArgumentList.Arguments.First()).Identifier.ValueText == entityType.Name);

                var description = requiredAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(RequiredEntityAttribute.Description))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                var displayName = requiredAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(RequiredEntityAttribute.DisplayName))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                var dependents = entityType.CustomAttributes
                    .Where(a => a.AttributeType.GetGenericTypeDefinition().Equals(typeof(DependentResourceAttribute<>)))
                    .Select(a => a.AttributeType.GenericTypeArguments.First())
                    .ToList();

                var crdDescription = new CrdDescription()
                {
                    Name        = $"{metadata.PluralName}.{metadata.Group}",
                    Version     = metadata.ApiVersion,
                    Kind        = metadata.Kind,
                    DisplayName = displayName,
                    Description = description
                };

                crdDescription.Resources = dependents?.Select(d => new ApiResourceReference()
                {
                    Kind = d.GetCustomAttribute<KubernetesEntityAttribute>().Kind,
                    Version = d.GetCustomAttribute<KubernetesEntityAttribute>().ApiVersion,
                    Name = d.GetCustomAttribute<KubernetesEntityAttribute>().PluralName
                }).ToList();

                results.Add(crdDescription);
            }

            return results;
        }

        public T GetAttribute<T>(
            MetadataLoadContext metadataLoadContext,
            Compilation compilation)
        {
            AttributeSyntax syntax = null;

            syntax = attributes
                .Where(a => a.Name.ToFullString() == typeof(T).Name)
                .FirstOrDefault();

            if (syntax == null)
            {
                var name = typeof(T).Name.Replace("Attribute", "");

                syntax = attributes
                    .Where(a => a.Name.ToFullString() == name)
                    .FirstOrDefault();
            }

            if (syntax == null)
            {
                return default(T);
            }

            return syntax.GetCustomAttribute<T>(metadataLoadContext, compilation);
        }

        public IEnumerable<T> GetAttributes<T>(
            MetadataLoadContext metadataLoadContext,
            Compilation compilation)
        {
            IEnumerable<AttributeSyntax> syntax = null;

            syntax = attributes
                .Where(a => a.Name.ToFullString() == typeof(T).Name);

            if (syntax == null || syntax.Count() == 0)
            {
                var name = typeof(T).Name.Replace("Attribute", "");

                syntax = attributes
                    .Where(a => a.Name.ToFullString() == name);
            }

            if (syntax == null || syntax.Count() == 0)
            {
                return null;
            }

            return syntax.Select(s => s.GetCustomAttribute<T>(metadataLoadContext, compilation));
        }
    }

    public static class OlmExtensions
    {
        public static T GetCustomAttribute<T>(
            this AttributeSyntax attributeData,
            MetadataLoadContext metadataLoadContext,
            Compilation compilation)
        {
            if (attributeData == null)
            {
                return default(T);
            }

            T attribute;

            // Check for constructor arguments
            if (attributeData.ArgumentList.Arguments.Any(a => a.NameEquals == null)
                && typeof(T).GetConstructors().Any(c => c.GetParameters().Length > 0))
            {
                // create instance with constructor args
                var actualArgs = attributeData.ArgumentList.Arguments
                    .Where(a => a.NameEquals == null)
                    .Select(a => a.Expression.ChildTokens().FirstOrDefault().Value).ToArray();

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
                var value        = p.Expression.GetExpressionValue<object>(metadataLoadContext);

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

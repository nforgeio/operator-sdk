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
            metadataLoadContext = new MetadataLoadContext(context.Compilation);
            attributes = ((OlmReceiver)context.SyntaxReceiver)?.Attributes;
            var namedTypeSymbols = context.Compilation.GetNamedTypeSymbols();

            var ownedEntities  = GetOwnedEntities();
            //var requiredEntities = GetRequiredEntities();

            var operatorName = GetAttribute<NameAttribute>(metadataLoadContext, context.Compilation);
            var displayName  = GetAttribute<DisplayNameAttribute>(metadataLoadContext, context.Compilation);
            var description  = GetAttribute<DescriptionAttribute>(metadataLoadContext, context.Compilation);
            var certified    = GetAttribute<CertifiedAttribute>(metadataLoadContext, context.Compilation)?.Certified ?? false;
            var createdData  = "";
            var containerImage = GetAttribute<ContainerImageAttribute>(metadataLoadContext, context.Compilation);
            var capabilities = GetAttribute<CapabilitiesAttribute>(metadataLoadContext, context.Compilation);
            var categories = GetAttribute<CategoryAttribute>(metadataLoadContext, context.Compilation);
            var icon = GetAttribute<IconAttribute>(metadataLoadContext, context.Compilation);
            var keywords = GetAttributes<KeywordAttribute>(metadataLoadContext, context.Compilation);
            var maintainers = GetAttributes<MaintainerAttribute>(metadataLoadContext, context.Compilation);
            var provider = GetAttribute<ProviderAttribute>(metadataLoadContext, context.Compilation);
            var version      = GetAttribute<VersionAttribute>(metadataLoadContext, context.Compilation);


            //if (operatorName == null
            //    || displayName == null)
            //{
            //    return;
            //}

            var csv = new V1ClusterServiceVersion().Initialize();

            csv.Metadata = new k8s.Models.V1ObjectMeta();
            csv.Metadata.Name = $"{operatorName.Name}.v{version.Version}";
            csv.Metadata.Annotations = new Dictionary<string, string>();
            csv.Metadata.Annotations.Add("description", description.ShortDescription);
            csv.Metadata.Annotations.Add("certified", certified.ToString().ToLower());
            csv.Spec = new V1ClusterServiceVersionSpec();
            csv.Spec.DisplayName = displayName.DisplayName;
            csv.Spec.Description = description.FullDescription;
            csv.Spec.Version = version.Version;
            csv.Spec.Provider = new Provider()
            {
                Name = provider.Name,
                Url = provider.Url
            };
            csv.Spec.Maintainers = maintainers.Select(m => new Maintainer()
            {
                Name = m.Name,
                Email = m.Email
            }).ToList();

            var outputString = KubernetesYaml.Serialize(csv);
            var outputPath = Path.Combine(targetDir, "clusterserviceversion.yaml");
            File.WriteAllText(outputPath, outputString);
        }

        public List<CrdDescription> GetOwnedEntities()
        {
            var results = new List<CrdDescription>();

            var ownedEntities = context.Compilation.Assembly.GetAttributes()
                .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith("OwnedEntity"));

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

            var ownedEntities = context.Compilation.Assembly.GetAttributes()
                .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith("RequiredEntity"));

            foreach (var entity in ownedEntities)
            {
                var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                var entityType = metadataLoadContext.ResolveType(etype);
                var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();
                var dependents = entityType.GetCustomAttributes(false)
                    .Where(a => a.GetType() == typeof(DependentResource<>))
                    .Select(a => a.GetType().GenericTypeArguments.First());

                var crdDescription = new CrdDescription()
                {
                    Name = $"{metadata.PluralName}.{metadata.Group}",
                    Version = metadata.ApiVersion,
                    Kind = metadata.Kind,
                    
                };

                crdDescription.Resources = dependents?.Select(d => new ApiResourceReference()
                {
                    Kind = d.GetKubernetesTypeMetadata().Kind,
                    Version = d.GetKubernetesTypeMetadata().ApiVersion,
                    Name = d.GetKubernetesTypeMetadata().PluralName
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
                return [default(T)];
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
                var value        = p.Expression.GetExpressionValue<string>(metadataLoadContext);

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

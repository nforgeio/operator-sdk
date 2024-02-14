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

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Operator.Analyzers.Receivers;
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

            var operatorName = GetAttribute<OperatorNameAttribute>();
            var displayName  = GetAttribute<OperatorDisplayNameAttribute>();
            var ownedEntities  = GetOwnedEntities();


            var csv = new V1ClusterServiceVersion().Initialize();

            csv.Metadata = new k8s.Models.V1ObjectMeta();
            csv.Metadata.Name = operatorName?.Name ?? context.Compilation.AssemblyName;
            csv.Spec = new V1ClusterServiceVersionSpec();
            csv.Spec.DisplayName = displayName?.DisplayName;

            var outputString = KubernetesYaml.Serialize(csv);
            var outputPath = Path.Combine(targetDir, "clusterserviceversion.yaml");
            File.WriteAllText(outputPath, outputString);
        }

        public List<IOwnedEntity> GetOwnedEntities()
        {
            var results = new List<IOwnedEntity>();

            var ownedEntities = context.Compilation.Assembly.GetAttributes()
                .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith("OwnedEntity"));

            foreach (var entity in ownedEntities)
            {
                var args       = entity.NamedArguments;
                var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                var entityType = metadataLoadContext.ResolveType(etype);
                var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                results.Add(new OwnedEntityAttribute(
                    name: $"{metadata.PluralName}.{metadata.Group}",
                    version: metadata.ApiVersion,
                    kind: metadata.Kind));
            }

            return results;
        }

        public T GetAttribute<T>()
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

            return syntax.GetCustomAttribute<T>();
        }
    }

    public static class OlmExtensions
    {
        public static T GetCustomAttribute<T>(this AttributeSyntax attributeData)
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
                var value        = p.Expression.ChildTokens().FirstOrDefault().Value;

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

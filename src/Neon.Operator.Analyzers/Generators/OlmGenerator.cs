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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Kubernetes.Resources.OperatorLifecycleManager;
using Neon.Operator.Analyzers.Receivers;
using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers.Generators
{
    [Generator]
    public class OlmGenerator : ISourceGenerator
    {
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


            var attrs = context.Compilation.Assembly.GetAttributes();
            var metadataLoadContext       = new MetadataLoadContext(context.Compilation);
            var olmAttributes             = ((OlmReceiver)context.SyntaxReceiver)?.Attributes;
            var namedTypeSymbols          = context.Compilation.GetNamedTypeSymbols();

            var operatorName = olmAttributes.GetAttribute<OperatorNameAttribute>();
            var displayName  = olmAttributes.GetAttribute<OperatorDisplayNameAttribute>();

            var csv = new V1ClusterServiceVersion().Initialize();

            csv.Metadata = new k8s.Models.V1ObjectMeta();
            csv.Metadata.Name = operatorName?.Name ?? context.Compilation.AssemblyName;
            csv.Spec = new V1ClusterServiceVersionSpec();
            csv.Spec.DisplayName = displayName?.DisplayName;

            var outputString = KubernetesYaml.Serialize(csv);
            var outputPath = Path.Combine(targetDir, "clusterserviceversion.yaml");
            File.WriteAllText(outputPath, outputString);
        }
    }

    public static class OlmExtensions
    {
        public static T GetCustomAttribute<T>(this AttributeSyntax attributeData)
            where T : class, new()
        {
            if (attributeData == null)
            {
                return default(T);
            }

            T attribute;
            attribute = (T)Activator.CreateInstance(typeof(T));

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

        public static T GetAttribute<T>(this List<AttributeSyntax> attributes)
            where T : Attribute, new()
        {
            var syntax = attributes
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
                return null;
            }

            return syntax.GetCustomAttribute<T>();
        }
    }
}

//-----------------------------------------------------------------------------
// FILE:	    CustomResourceGenerator.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.Rest.Serialization;

using Neon.Common;
using Neon.Operator.Attributes;
using Neon.Operator.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;

using YamlDotNet.Serialization;

namespace Neon.Operator.Entities
{
    /// <summary>
    /// A tool for generating Kubernetes Custom Resources.
    /// </summary>
    public class CustomResourceGenerator
    {
        private readonly JsonSchemaGeneratorSettings jsonSchemaGeneratorSettings;
        private readonly JsonSerializerSettings      serializerSettings;

        private AssemblyScanner                      assemblyScanner;
        private IEnumerable<string>                  kubernetesTypes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxDepth">Optionally specifies the maximum depth when parsing JSON.</param>
        /// <param name="converters">Optionall specifies any JSON converters.</param>
        public CustomResourceGenerator(int maxDepth = 128, IEnumerable<JsonConverter> converters = null)
        {
            serializerSettings = new JsonSerializerSettings
            {
                Formatting            = Formatting.None,
                DateFormatHandling    = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling  = DateTimeZoneHandling.Utc,
                NullValueHandling     = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver      = new CamelCasePropertyNamesContractResolver(),
                Converters            = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter(),
                    new StringEnumConverter()
                },
                MaxDepth = maxDepth,
            };

            if (converters != null)
            {
                foreach (var converter in converters)
                {
                    serializerSettings.Converters.Add(converter);
                }
            }

            jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings()
            {
                SchemaType  = SchemaType.OpenApi3,
                TypeMappers =
                {
                    new ObjectTypeMapper(typeof(V1ObjectMeta), new JsonSchema { Type = JsonObjectType.Object}),
                },
                SerializerSettings = serializerSettings,
                
            };


        }

        /// <summary>
        /// Generates a <see cref="V1CustomResourceDefinition"/> for a Kubernetes custom resource entity.
        /// </summary>
        /// <param name="resourceType">Specifies the resource type.</param>
        /// <param name="entity"></param>
        /// <param name="versionAttribute"></param>
        /// <param name="scaleAttribute"></param>
        /// <returns>The <see cref="V1CustomResourceDefinition"/>.</returns>
        public V1CustomResourceDefinition GenerateCustomResourceDefinition(
            Type                      resourceType,
            KubernetesEntityAttribute entity           = null,
            EntityVersionAttribute    versionAttribute = null,
            ScaleAttribute            scaleAttribute   = null)
        {
            Covenant.Requires<ArgumentNullException>(resourceType != null, nameof(resourceType));

            var scope            = GetScope(resourceType) ?? EntityScope.Namespaced;

            entity               ??= resourceType.GetTypeInfo().GetCustomAttribute<KubernetesEntityAttribute>();

            try
            {
                versionAttribute ??= resourceType.GetTypeInfo().GetCustomAttribute<EntityVersionAttribute>();
            }
            catch
            {
                versionAttribute = new EntityVersionAttribute() { Served = true, Storage = true };
            }

            try
            {
                scaleAttribute ??= resourceType.GetCustomAttribute<ScaleAttribute>();
            }
            catch
            {
                // no scale attribute
                scaleAttribute = null;
            }

            var schema           = GenerateJsonSchema(resourceType);
            var pluralNameGroup  = string.IsNullOrEmpty(entity.Group) ? entity.PluralName : $"{entity.PluralName}.{entity.Group}";

            var implementsStatus = resourceType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStatus<> ));

            var crd = new V1CustomResourceDefinition(
                apiVersion: $"{V1CustomResourceDefinition.KubeGroup}/{V1CustomResourceDefinition.KubeApiVersion}",
                kind:       V1CustomResourceDefinition.KubeKind,
                metadata:   new V1ObjectMeta(name: pluralNameGroup),
                spec:       new V1CustomResourceDefinitionSpec(
                group:      entity.Group,
                names:      new V1CustomResourceDefinitionNames(kind: entity.Kind, plural: entity.PluralName),
                scope:      scope.ToMemberString(),
                versions:   new List<V1CustomResourceDefinitionVersion>
                {
                    new V1CustomResourceDefinitionVersion(
                        name:         entity.ApiVersion,
                        served:       versionAttribute?.Served ?? true,
                        storage:      versionAttribute?.Storage ?? true,
                        schema:       new V1CustomResourceValidation(schema),
                        subresources: new V1CustomResourceSubresources()
                        {
                            Status = implementsStatus ? new object() : null,
                            Scale  = scaleAttribute != null
                                ? new V1CustomResourceSubresourceScale()
                                  {
                                      LabelSelectorPath  = scaleAttribute.LabelSelectorPath,
                                      SpecReplicasPath   = scaleAttribute.SpecReplicasPath,
                                      StatusReplicasPath = scaleAttribute.StatusReplicasPath,
                                  }
                                : null
                        }),
                }));

            foreach (var version in crd.Spec.Versions)
            {
                if (version.Schema.OpenAPIV3Schema.Properties != null &&
                    version.Schema.OpenAPIV3Schema.Properties.TryGetValue("metadata", out var metadata))
                {
                    metadata.Description = null;
                }
            }

            return crd;
        }

        /// <summary>
        /// Returns custom resource definitions from an assembly.
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<V1CustomResourceDefinition> GetCustomResourcesFromAssembly(
            string           assemblyPath)
        {
            this.assemblyScanner ??= new AssemblyScanner();
            this.kubernetesTypes ??= Assembly.GetAssembly(typeof(V1Pod)).DefinedTypes.Where(t => t.GetCustomAttribute<KubernetesEntityAttribute>() != null).Select(t => t.GetKubernetesCrdName());
            
            assemblyScanner.Add(assemblyPath);
            var customResourceDefinitions = new List<V1CustomResourceDefinition>();

            foreach (var type in assemblyScanner.EntityTypes)
            {
                var crdName = type.GetKubernetesCrdName();

                if (kubernetesTypes.Contains(crdName))
                {
                    continue;
                }

                var customResourceDefinition = GenerateCustomResourceDefinition(type);
                if (customResourceDefinitions.Any(crd => crd.Name() == customResourceDefinition.Name()))
                {
                    customResourceDefinitions
                        .Where(crd => crd.Name() == customResourceDefinition.Name()
                                        && !crd.Spec.Versions.Any(v => customResourceDefinition.Spec.Versions.Select(v => v.Name).Contains(v.Name)))
                        .FirstOrDefault()?
                        .Spec.Versions.Add(customResourceDefinition.Spec.Versions.FirstOrDefault());
                }
                else
                {
                    customResourceDefinitions.Add(customResourceDefinition);
                }
            }

            var duplicateStoredVersions = customResourceDefinitions.Where(crd => crd.Spec.Versions.Where(v => v.Storage).Count() > 1);
            if (duplicateStoredVersions.Any())
            {
                throw new Exception($"Only 1 stored version allowed. The following resources have multiple stored versions: [{string.Join(", ", duplicateStoredVersions.Select(crd => crd.Name()))}]");
            }

            return customResourceDefinitions;
        }

        /// <summary>
        /// Writes a <see cref="V1CustomResourceDefinition"/> to a file.
        /// </summary>
        /// <param name="resourceDefinition">The Custom Resource Definition.</param>
        /// <param name="path">The file path.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public async Task WriteToFile(V1CustomResourceDefinition resourceDefinition, string path)
        {
            Covenant.Requires<ArgumentNullException>(resourceDefinition != null, nameof(resourceDefinition));
            Covenant.Requires<ArgumentNullException>(path != null, nameof(path));

            using (var file = File.Create(path))
            {
                using (var writer = new StreamWriter(file))
                {
                    await writer.WriteLineAsync(KubernetesYaml.Serialize(resourceDefinition));
                }
            }
        }

        /// <summary>
        /// Returns the cluster scope for a resource type.
        /// </summary>
        /// <param name="resourceType">Specifies the resource type.</param>
        /// <returns>The <see cref="EntityScope"/> or <c>null</c> when this can't be determined.</returns>
        private EntityScope? GetScope(Type resourceType)
        {
            Covenant.Requires<ArgumentNullException>(resourceType != null, nameof(resourceType));

            try
            {
                return resourceType.GetCustomAttribute<EntityScopeAttribute>().Scope;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generates a schema from a resource type.
        /// </summary>
        /// <param name="resourceType">Specififies the resource type.</param>
        /// <returns>The generated <see cref="V1JSONSchemaProps"/>.</returns>
        private V1JSONSchemaProps GenerateJsonSchema(Type resourceType)
        {
            Covenant.Requires<ArgumentNullException>(resourceType != null, nameof(resourceType));

            // Start with JsonSchema.

            var g = new JsonSchemaGenerator(jsonSchemaGeneratorSettings);

            g.Generate(resourceType);

            var schema = JsonSchema.FromType(resourceType, jsonSchemaGeneratorSettings);
            // Convert to JToken to make alterations.

            // $todo(marcusbooyah):
            //
            //      * This seems janky: why are we setting [rootToken] and then not using it?
            //      * Seems like [sRootToken] could be renamed to claify what it is.

            var rootToken  = JObject.Parse(schema.ToJson());
            var sRootToken = JsonObject.Create(JsonDocument.Parse(schema.ToJson()).RootElement);

            sRootToken = RewriteObject(sRootToken);

            sRootToken.Remove("$schema");
            sRootToken.Remove("definitions");

            var schemaProps = sRootToken.Deserialize<V1JSONSchemaProps>();

            return schemaProps;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <returns>The <see cref="JsonObject"/>.</returns>
        private JsonObject RewriteObject(JsonObject sourceObject)
        {
            Covenant.Requires<ArgumentNullException>(sourceObject != null, nameof(sourceObject));

            var targetObject = new JsonObject();
            var queue        = new Queue<JsonObject>();

            queue.Enqueue(sourceObject);

            while (queue.Count != 0)
            {
                sourceObject = queue.Dequeue();

                foreach (var property in sourceObject.AsEnumerable())
                {
                    if (property.Key == "$ref")
                    {
                        // Resolve the target of the "$ref".

                        var reference = sourceObject;

                        foreach (var part in property.Value.GetValue<string>().Split('/'))
                        {
                            if (part == "#")
                            {
                                reference = (JsonObject)reference.Root;
                            }
                            else if (reference.TryGetPropertyValue(part, out var propertyValue))
                            {
                                reference = (JsonObject)propertyValue;
                            }
                        }

                        // The referenced object should be merged into the current target.

                        queue.Enqueue(reference);

                        // ...and $ref property is not added.

                        continue;
                    }

                    if (property.Key == "additionalProperties")
                    {
                        try
                        {
                            var pValue = property.Value.GetValue<bool>();

                            if (!pValue)
                            {
                                continue;
                            }
                        }
                        catch
                        {
                            // Ignoring
                        }
                    }

                    if (property.Key == "oneOf" && property.Value is JsonArray && property.Value.AsArray().Count == 1)
                    {
                        // A single oneOf array item should be merged into current object.

                        queue.Enqueue(RewriteObject((JsonObject)property.Value.AsArray().Single()));

                        // ...and don't add the [oneOf] property
                        continue;
                    }

                    // All other properties are added after the value is rewritten recursively.

                    if (!targetObject.TryGetPropertyValue(property.Key, out var value))
                    {
                        targetObject.Add(property.Key, RewriteToken(property.Value));
                    }
                }
            }

            return targetObject;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <param name="sourceToken"></param>
        /// <returns>The <see cref="JsonNode"/>.</returns>
        private JsonNode RewriteToken(JsonNode sourceToken)
        {
            Covenant.Requires<ArgumentNullException>(sourceToken != null, nameof(sourceToken));

            if (sourceToken is JsonObject sourceObject)
            {
                return RewriteObject(sourceObject);
            }
            else if (sourceToken is JsonArray sourceArray)
            {
                return new JsonArray(sourceArray.Select(RewriteToken).ToArray());
            }
            else
            {
                return JsonNode.Parse(sourceToken.ToJsonString());
            }
        }
    }
}

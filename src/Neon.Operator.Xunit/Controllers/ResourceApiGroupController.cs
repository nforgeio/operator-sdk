//-----------------------------------------------------------------------------
// FILE:	    ResourceApiGroupController.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using Neon.Common;
using Neon.K8s;
using Neon.Tasks;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Generic resource API controller.
    /// </summary>
    [Route("apis/{group}/{version}/{plural}")]
    [Route("apis/{group}/{version}/{plural}/{name}")]
    [Route("apis/{group}/{version}/namespaces/{namespace}/{plural}")]
    [Route("apis/{group}/{version}/namespaces/{namespace}/{plural}/{name}")]
    public class ResourceApiGroupController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly ITestApiServer         testApiServer;
        private readonly JsonSerializerOptions  jsonSerializerOptions;

        /// <summary>
        /// Constructors.
        /// </summary>
        /// <param name="testApiServer">Specifies the test API server.</param>
        /// <param name="jsonSerializerOptions">Specifies the JSON serializer options.</param>
        public ResourceApiGroupController(ITestApiServer testApiServer, JsonSerializerOptions jsonSerializerOptions)
        {
            Covenant.Requires<ArgumentNullException>(testApiServer != null, nameof(testApiServer));
            Covenant.Requires<ArgumentNullException>(jsonSerializerOptions != null, nameof(jsonSerializerOptions));

            this.testApiServer         = testApiServer;
            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        /// <summary>
        /// Specifies the group for the <see cref="IKubernetesObject"/>.
        /// </summary>
        [FromRoute]
        public string Group { get; set; }

        /// <summary>
        /// Specifies the custom resource version. <see cref="IKubernetesObject.ApiVersion"/>.
        /// </summary>
        [FromRoute]
        public string Version { get; set; }

        /// <summary>
        /// Specifies the plural name of the <see cref="IKubernetesObject"/>.
        /// </summary>
        [FromRoute]
        public string Plural { get; set; }

        /// <summary>
        /// Specifies the namespace name of the <see cref="IKubernetesObject"/>.
        /// </summary>
        [FromRoute]
        public string Namespace { get; set; }

        /// <summary>
        /// Specifies the name name of the <see cref="IKubernetesObject"/>.
        /// </summary>
        [FromRoute]
        public string Name { get; set; }

        /// <summary>
        /// Fetches the list of resources held by the test API server.
        /// </summary>
        /// <returns>An action result containing the resources.</returns>
        [HttpGet]
        public async Task<ActionResult> GetAsync()
        {
            await SyncContext.Clear;

            var key = $"{Group}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata = type.GetKubernetesTypeMetadata();

                if (Name == null)
                {
                    var     resources                   = testApiServer.Resources.Where(r => r.GetType() == type);
                    var     CustomObjectListType        = typeof(V1CustomObjectList<>);
                    Type[]  typeArgs                    = { type };
                    var     customObjectListGenericType = CustomObjectListType.MakeGenericType(typeArgs);
                    dynamic customObjectList            = Activator.CreateInstance(customObjectListGenericType);
                    var     iListType                   = typeof(IList<>);
                    var     iListGenericType            = iListType.MakeGenericType(typeArgs);
                    var     stringList                  = JsonSerializer.Serialize(resources, jsonSerializerOptions);
                    var     result                      = (dynamic)JsonSerializer.Deserialize(stringList, iListGenericType, jsonSerializerOptions);

                    customObjectList.Items = result;

                    return Ok(customObjectList);
                }
                else
                {
                    var resource = testApiServer.Resources.Where(resource => resource.Kind == typeMetadata.Kind && resource.Metadata.Name == Name)
                        .FirstOrDefault();

                    if (resource == null)
                    {
                        return NotFound();
                    }

                    return Ok(resource);
                }
            }

            return NotFound();
        }

        /// <summary>
        /// Creates a resource and stores it in <see cref="TestApiServer.Resources"/>
        /// </summary>
        /// <param name="resource">Specifies the resource.</param>
        /// <returns>An action result containing the resource.</returns>
        [HttpPost]
        public async Task<ActionResult<ResourceObject>> CreateAsync([FromBody] object resource)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            var key = $"{Group}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata = type.GetKubernetesTypeMetadata();
                var json         = JsonSerializer.Serialize(resource, jsonSerializerOptions);
                var instance     = JsonSerializer.Deserialize(json, type, jsonSerializerOptions);

                testApiServer.AddResource(Group, Version, Plural, typeMetadata.Kind, instance, Namespace);

                return Ok(resource);
            }

            return NotFound();
        }

        /// <summary>
        /// Replaces a resource and stores it in <see cref="TestApiServer.Resources"/>
        /// </summary>
        /// <param name="resource">Specifies the replacement resource.</param>
        /// <returns>An action result containing the resource.</returns>
        [HttpPut]
        public async Task<ActionResult<ResourceObject>> UpdateAsync([FromBody] object resource)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            var key = $"{Group}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata  = type.GetKubernetesTypeMetadata();
                var json          = JsonSerializer.Serialize(resource, jsonSerializerOptions);
                var instance      = JsonSerializer.Deserialize(json, type, jsonSerializerOptions);
                var resourceQuery = testApiServer.Resources.Where(resource => resource.Kind == typeMetadata.Kind && resource.Metadata.Name == Name);

                if (!string.IsNullOrEmpty(Namespace))
                {
                    resourceQuery = resourceQuery.Where(r => r.EnsureMetadata().NamespaceProperty == Namespace);
                }

                var existing = resourceQuery.SingleOrDefault();

                if (existing == null)
                {
                    return NotFound();
                }

                testApiServer.Resources.Remove(existing);
                testApiServer.AddResource(Group, Version, Plural, typeMetadata.Kind, instance, Namespace);

                return Ok(resource);
            }

            return NotFound();
        }

        /// <summary>
        /// Patches a resource in <see cref="TestApiServer.Resources"/>
        /// </summary>
        /// <param name="patchDoc">Specifies the PATCH document.</param>
        /// <returns>An action result containing the resource.</returns>
        [HttpPatch]
        public async Task<ActionResult<ResourceObject>> PatchAsync(
            [FromBody] JsonPatchDocument<IKubernetesObject<V1ObjectMeta>> patchDoc)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(patchDoc != null, nameof(patchDoc));

            var key = $"{Group}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata = type.GetKubernetesTypeMetadata();
                var resources    = testApiServer.Resources.Where(resource => resource.Kind == typeMetadata.Kind && resource.Metadata.Name == Name)
                    .ToList();

                var resource = resources.FirstOrDefault();

                if (resource == null)
                {
                    return NotFound();
                }

                patchDoc.ApplyTo(resource);

                return Ok(resource);
            }

            return NotFound();
        }

        /// <summary>
        /// Deletes a resource in <see cref="TestApiServer.Resources"/>
        /// </summary>
        /// <returns>An action result containing the resource.</returns>
        [HttpDelete]
        public async Task<ActionResult> DeleteAsync()
        {
            await SyncContext.Clear;

            var key = $"{Group}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata = type.GetKubernetesTypeMetadata();
                var resources    = testApiServer.Resources.Where(resource => resource.Kind == typeMetadata.Kind && resource.Metadata.Name == Name).ToList();

                if (resources.IsEmpty())
                {
                    return NotFound();
                }

                foreach (var resource in resources)
                {
                    testApiServer.Resources.Remove(resource);

                    if (resource.Metadata.OwnerReferences != null)
                    {
                        foreach (var child in resource.Metadata.OwnerReferences)
                        {
                            testApiServer.Resources.Remove(testApiServer.Resources
                                .Where(resource => resource.Uid() == child.Uid)
                                .Single());
                        }
                    }
                }

                var status = new V1Status().Initialize();
                status.Message = "deleted";
                status.Status = "deleted";
                status.Code = 200;

                return Content(NeonHelper.JsonSerialize(status), "application/json");
            }

            return NotFound();
        }
    }
}

//-----------------------------------------------------------------------------
// FILE:	    ResourceApiController.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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
using System.Resources;
using System.Text.Json;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using Neon.Common;
using Neon.K8s;
using Neon.Tasks;

using OpenTelemetry.Resources;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Generic resource API controller. 
    /// </summary>
    [Route("api/{version}/{plural}")]
    [Route("api/{version}/{plural}/{name}")]
    [Route("api/{version}/namespaces/{namespace}/{plural}")]
    [Route("api/{version}/namespaces/{namespace}/{plural}/{name}")]
    public class ResourceApiController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly ITestApiServer         testApiServer;
        private readonly JsonSerializerOptions  jsonSerializerOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="testApiServer">Specifies the test API swerver.</param>
        /// <param name="jsonSerializerOptions">Specifies the JSON serializer options.</param>
        public ResourceApiController(ITestApiServer testApiServer, JsonSerializerOptions jsonSerializerOptions)
        {
            Covenant.Requires<ArgumentNullException>(testApiServer != null, nameof(testApiServer));
            Covenant.Requires<ArgumentNullException>(jsonSerializerOptions != null, nameof(jsonSerializerOptions));

            this.testApiServer         = testApiServer;
            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        /// <summary>
        /// The custom resource version. <see cref="IKubernetesObject.ApiVersion"/>.
        /// </summary>
        [FromRoute]
        public string Version { get; set; }

        /// <summary>
        /// The plural name of the <see cref="IKubernetesObject"/>.
        /// </summary>
        [FromRoute]
        public string Plural { get; set; }

        /// <summary>
        /// The namespace name of the <see cref="IKubernetesObject"/>.
        /// </summary>
        [FromRoute]
        public string Namespace { get; set; }

        /// <summary>
        /// The name of the <see cref="IKubernetesObject"/>.
        /// </summary>
        [FromRoute]
        public string Name { get; set; }

        /// <summary>
        /// Gets a resource as a list or by name <see cref="TestApiServer.Resources"/>
        /// </summary>
        /// <returns>An action result containing the resource.</returns>
        [HttpGet]
        public async Task<ActionResult> GetAsync()
        {
            await SyncContext.Clear;

            var key = $"{string.Empty}/{Version}/{Plural}";

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
        /// <param name="resource">Specifies the new resource.</param>
        /// <returns>An action result containing the resource.</returns>
        [HttpPost]
        public async Task<ActionResult<ResourceObject>> CreateAsync([FromBody] object resource)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            var key = $"{string.Empty}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata = type.GetKubernetesTypeMetadata();
                var serializer   = JsonSerializer.Serialize(resource, jsonSerializerOptions);
                var instance     = JsonSerializer.Deserialize(serializer, type, jsonSerializerOptions);

                testApiServer.AddResource(string.Empty, Version, Plural, typeMetadata.Kind, instance, Namespace);

                return Ok(resource);
            }

            return NotFound();
        }

        /// <summary>
        /// Replaces a resource and stores it in <see cref="TestApiServer.Resources"/>
        /// </summary>
        /// <param name="resource">Specifies the replacement respource.</param>
        /// <returns>An action result containing the resource.</returns>
        [HttpPut]
        public async Task<ActionResult<ResourceObject>> UpdateAsync([FromBody] object resource)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            var key = $"{string.Empty}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata  = type.GetKubernetesTypeMetadata();
                var serializer    = JsonSerializer.Serialize(resource, jsonSerializerOptions);
                var instance      = JsonSerializer.Deserialize(serializer, type, jsonSerializerOptions);
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
                testApiServer.AddResource(string.Empty, Version, Plural, typeMetadata.Kind, instance, Namespace);

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
        public async Task<ActionResult<ResourceObject>> PatchAsync([FromBody] JsonPatchDocument<IKubernetesObject<V1ObjectMeta>> patchDoc)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(patchDoc != null, nameof(patchDoc));

            var key = $"{string.Empty}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata  = type.GetKubernetesTypeMetadata();
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

                patchDoc.ApplyTo(existing);

                return Ok(existing);
            }

            return NotFound();
        }

        /// <summary>
        /// Deletes a resource in <see cref="TestApiServer.Resources"/>
        /// </summary>
        /// <returns>An action result containing the resource.</returns>
        [HttpDelete]
        public async Task<ActionResult<V1Status>> DeleteAsync()
        {
            await SyncContext.Clear;

            var key = $"{string.Empty}/{Version}/{Plural}";

            if (testApiServer.Types.TryGetValue(key, out Type type))
            {
                var typeMetadata  = type.GetKubernetesTypeMetadata();
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

                if (existing.Metadata.OwnerReferences != null)
                {
                    foreach (var child in existing.Metadata.OwnerReferences)
                    {
                        testApiServer.Resources.Remove(testApiServer.Resources.Where(r => r.Uid() == child.Uid).Single());
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

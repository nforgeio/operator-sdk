//-----------------------------------------------------------------------------
// FILE:	    ResourceListController.cs
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
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using Neon.Common;
using Neon.K8s;
using Neon.K8s.Core;
using Neon.Operator.Attributes;
using Neon.Tasks;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Generic resource API controller.
    /// </summary>
    public class ResourceListController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly ITestApiServer         testApiServer;
        private readonly JsonSerializerOptions  jsonSerializerOptions;

        /// <summary>
        /// Constructors.
        /// </summary>
        /// <param name="testApiServer">Specifies the test API server.</param>
        /// <param name="jsonSerializerOptions">Specifies the JSON serializer options.</param>
        public ResourceListController(ITestApiServer testApiServer, JsonSerializerOptions jsonSerializerOptions)
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
        /// Fetches the list of resources held by the test API server.
        /// </summary>
        /// <returns>An action result containing the resources.</returns>
        [HttpGet("apis/{group}/{version}")]
        public async Task<ActionResult> GetGroupAsync()
        {
            await SyncContext.Clear;

            var key = ApiHelper.CreateKey(Group, Version);

            var types = testApiServer.Types.Where(t => t.Key.StartsWith(key));

            if (types.IsEmpty())
            {
                return NotFound();
            }

            var result = new V1APIResourceList().Initialize();

            result.ApiVersion = "v1";
            result.GroupVersion = key;
            result.Resources = new List<V1APIResource>();

            foreach (var t in types)
            {
                var metadata    = t.Value.GetKubernetesTypeMetadata();
                var entityScope = t.Value.GetCustomAttribute<EntityScopeAttribute>();

                result.Resources.Add(
                    new V1APIResource()
                    {
                        Name = metadata.PluralName,
                        SingularName = metadata.PluralName.ToLower(),
                        Namespaced = !(entityScope?.Scope == EntityScope.Cluster),
                        Kind = metadata.Kind,
                        Verbs =
                        [
                            "delete",
                            "deletecollection",
                            "get",
                            "list",
                            "patch",
                            "create",
                            "update",
                            "watch"
                        ],
                    });

                var interfaces = t.Value.GetInterfaces();

                foreach (var i in interfaces)
                {
                    if (!i.IsGenericType)
                    {
                        continue;
                    }

                    var eq = i.GetGenericTypeDefinition().IsEquivalentTo(typeof(IStatus<>));
                }

                if (t.Value
                    .GetInterfaces()?
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().IsEquivalentTo(typeof(IStatus<>))) == true)
                {
                    var statusType =
                        t.Value
                        .GetInterfaces()
                        .Where(i =>
                            i.IsGenericType
                            && i.GetGenericTypeDefinition().IsEquivalentTo(typeof(IStatus<>)))
                        .FirstOrDefault()
                        .GenericTypeArguments
                        .FirstOrDefault();

                    result.Resources.Add(
                        new V1APIResource()
                        {
                            Name = $"{metadata.PluralName}/status",
                            SingularName = string.Empty,
                            Namespaced = !(entityScope?.Scope == EntityScope.Cluster),
                            Kind = metadata.Kind,
                            Verbs =
                            [
                                "get",
                                "patch",
                                "update"
                            ],
                        });

                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Fetches the list of resources held by the test API server.
        /// </summary>
        /// <returns>An action result containing the resources.</returns>
        [HttpGet("api/{version}")]
        public async Task<ActionResult> GetApisAsync()
        {
            await SyncContext.Clear;

            var key = ApiHelper.CreateKey(Version);

            var types = testApiServer.Types.Where(t => t.Key.StartsWith(key));

            if (types.IsEmpty())
            {
                return NotFound();
            }

            var result = new V1APIResourceList().Initialize();

            result.ApiVersion = "v1";
            result.GroupVersion = key;
            result.Resources = new List<V1APIResource>();

            foreach (var t in types)
            {
                var metadata    = t.Value.GetKubernetesTypeMetadata();
                var entityScope = t.Value.GetCustomAttribute<EntityScopeAttribute>();

                result.Resources.Add(
                    new V1APIResource()
                    {
                        Name = metadata.PluralName,
                        SingularName = metadata.PluralName.ToLower(),
                        Namespaced = !(entityScope?.Scope == EntityScope.Cluster),
                        Kind = metadata.Kind,
                        Verbs =
                        [
                            "delete",
                            "deletecollection",
                            "get",
                            "list",
                            "patch",
                            "create",
                            "update",
                            "watch"
                        ],
                    });

                var interfaces = t.Value.GetInterfaces();

                foreach (var i in interfaces)
                {
                    if (!i.IsGenericType)
                    {
                        continue;
                    }

                    var eq = i.GetGenericTypeDefinition().IsEquivalentTo(typeof(IStatus<>));
                }

                if (t.Value
                    .GetInterfaces()?
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().IsEquivalentTo(typeof(IStatus<>))) == true)
                {
                    var statusType =
                        t.Value
                        .GetInterfaces()
                        .Where(i =>
                            i.IsGenericType
                            && i.GetGenericTypeDefinition().IsEquivalentTo(typeof(IStatus<>)))
                        .FirstOrDefault()
                        .GenericTypeArguments
                        .FirstOrDefault();

                    result.Resources.Add(
                        new V1APIResource()
                        {
                            Name = $"{metadata.PluralName}/status",
                            SingularName = string.Empty,
                            Namespaced = !(entityScope?.Scope == EntityScope.Cluster),
                            Kind = metadata.Kind,
                            Verbs =
                            [
                                "get",
                                "patch",
                                "update"
                            ],
                        });

                }
            }

            return Ok(result);
        }
    }
}

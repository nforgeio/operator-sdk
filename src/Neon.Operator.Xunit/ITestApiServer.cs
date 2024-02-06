// FILE:	    ITestApiServer.cs
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
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Http;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Test API server.
    /// </summary>
    public interface ITestApiServer
    {
        /// <summary>
        /// The resources contained by the API server.
        /// </summary>
        List<IKubernetesObject<V1ObjectMeta>> Resources { get; }

        /// <summary>
        /// The types that the API server recognises.
        /// </summary>
        Dictionary<string, Type> Types { get; }

        /// <summary>
        /// Called for unhandled requests.
        /// </summary>
        /// <param name="context">Specifies the request context.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        Task UnhandledRequest(HttpContext context);

        /// <summary>
        /// Adds a resource to the API server's resource collection.
        /// </summary>
        /// <param name="group">Specifies the API group.</param>
        /// <param name="version">Specifies the API version.</param>
        /// <param name="plural">Specifies the plural name for the resource.</param>
        /// <param name="kind">Specifies the resource kind.</param>
        /// <param name="resource">Specifies the resource.</param>
        /// <param name="namespaceParameter">Specifies the resource namespace.</param>
        void AddResource(string group, string version, string plural, string kind, object resource, string namespaceParameter = null);

        /// <summary>
        /// Adds a type-safe resource to the API server's resource collection.
        /// </summary>
        /// <typeparam name="TResource">Specifies the resource type.</typeparam>
        /// <param name="resource">Specifies the resource.</param>
        /// <param name="namespaceParameter">Specifies the resource namespace.</param>
        void AddResource<TResource>(TResource resource, string namespaceParameter = null)
            where TResource : IKubernetesObject<V1ObjectMeta>;
    }
}

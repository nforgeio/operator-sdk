// FILE:	    TestApiServer.cs
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
using System.Text.Json;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Neon.Operator.Xunit
{
    /// <inheritdoc/>
    public class TestApiServer : ITestApiServer
    {
        private JsonSerializerOptions   jsonSerializerOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Specifies the test API server options.</param>
        /// <param name="jsonSerializerOptions">Specifies the JSON serialization options.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TestApiServer(IOptions<TestApiServerOptions> options, JsonSerializerOptions jsonSerializerOptions)
        {
            Covenant.Requires<ArgumentNullException>(options != null, nameof(options));
            Covenant.Requires<ArgumentNullException>(jsonSerializerOptions != null, nameof(jsonSerializerOptions));

            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        /// <inheritdoc/>
        public List<IKubernetesObject<V1ObjectMeta>> Resources { get; } = new List<IKubernetesObject<V1ObjectMeta>>();

        /// <inheritdoc/>
        public Dictionary<string, Type> Types { get; } = new Dictionary<string, Type>();

        /// <inheritdoc/>
        public virtual Task UnhandledRequest(HttpContext context)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void AddResource(string group, string version, string plural, string kind, object resource, string namespaceParameter = null)
        {
            var k8sObj = EnsureMetadata(resource);

            if (string.IsNullOrEmpty(k8sObj.Kind))
            {
                k8sObj.Kind = kind;
            }

            if (!string.IsNullOrEmpty(namespaceParameter))
            {
                if (!string.IsNullOrEmpty(k8sObj.Metadata?.NamespaceProperty) &&
                    namespaceParameter != k8sObj.Metadata?.NamespaceProperty)
                {
                    throw new ArgumentException("Namespace mismatch");
                }
                else
                {
                    k8sObj.EnsureMetadata();
                    k8sObj.Metadata.NamespaceProperty = namespaceParameter;
                }
            }
            Resources.Add(k8sObj);
        }

        /// <inheritdoc/>
        public virtual void AddResource<T>(T resource, string namespaceParameter = null)
            where T : IKubernetesObject<V1ObjectMeta>
        {
            var typeMetadata = typeof(T).GetKubernetesTypeMetadata();

            var serializer = JsonSerializer.Serialize(resource);
            var instance   = (T)JsonSerializer.Deserialize(serializer, typeof(T), jsonSerializerOptions);

            AddResource(typeMetadata.Group, typeMetadata.ApiVersion, typeMetadata.PluralName, typeMetadata.Kind, instance, namespaceParameter);
        }

        private IKubernetesObject<V1ObjectMeta> EnsureMetadata(object _object)
        {
            var resource = (IKubernetesObject<V1ObjectMeta>)_object;

            if (resource.Uid() == null)
            {
                resource.Metadata.Uid = Guid.NewGuid().ToString();
            }

            return resource;
        }
    }
}

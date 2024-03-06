//-----------------------------------------------------------------------------
// FILE:	    CrdCache.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Text;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Logging;

using Neon.Diagnostics;

using OpenTelemetry.Resources;
using YamlDotNet.Core;

namespace Neon.Operator.Cache
{
    /// <summary>
    /// Used to cache CRDs for improved performance.
    /// </summary>
    internal class CrdCache : ICrdCache
    {
        private readonly ILogger<CrdCache>                                        logger;
        private readonly ConcurrentDictionary<string, V1APIResource> cache;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="loggerFactory">Optionally specifies a logger factory.</param>
        public CrdCache(ILoggerFactory loggerFactory = null) 
        {

            this.cache   = new ConcurrentDictionary<string, V1APIResource>();
            this.logger  = loggerFactory?.CreateLogger<CrdCache>();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            cache.Clear();
        }

        private static string CreateKey(KubernetesEntityAttribute metadata)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(metadata.Group))
            {
                sb.Append(metadata.Group);
                sb.Append('/');
            }

            sb.Append(metadata.ApiVersion);
            sb.Append('/');
            sb.Append(metadata.PluralName);
            return sb.ToString();
        }

        private static string CreateKey(string group, string version, string pluralName)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(group))
            {
                sb.Append(group);
                sb.Append('/');
            }

            sb.Append(version);
            sb.Append('/');
            sb.Append(pluralName);
            return sb.ToString();
        }

        private static string CreateKey(string groupVersion, string pluralName)
        {
            return $"{groupVersion}/{pluralName}";
        }

        /// <inheritdoc/>
        public V1APIResource Get(KubernetesEntityAttribute metadata)
        {
            Covenant.Requires<ArgumentNullException>(metadata != null, nameof(metadata));

            var result = cache.GetValueOrDefault(CreateKey(metadata));

            return result;
        }

        /// <inheritdoc/>
        public V1APIResource Get<TResource>()
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var result = cache.GetValueOrDefault(CreateKey(typeof(TResource).GetKubernetesTypeMetadata()));

            return result;
        }

        /// <inheritdoc/>
        public void Remove(string group, string version, V1APIResource resource)
        {
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            cache.Remove(CreateKey(group, version, resource.Name), out _);
        }

        /// <inheritdoc/>
        public void Upsert(string group, string version, V1APIResource resource)
        {
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            var id = CreateKey(group, version, resource.Name);

            logger?.LogDebugEx(() => $"Adding {id} to cache.");

            cache.AddOrUpdate(
                key: id,
                addValueFactory: (id) =>
                {
                    return Clone(resource);
                },
                updateValueFactory: (key, oldresource) =>
                {
                    return Clone(resource);
                });
        }

        /// <inheritdoc/>
        public void Upsert(V1APIResourceList resources)
        {
            Covenant.Requires<ArgumentNullException>(resources != null, nameof(resources));

            foreach (var resource in resources.Resources)
            {
                var id = CreateKey(resources.GroupVersion, resource.Name);

                logger?.LogDebugEx(() => $"Adding {id} to cache.");

                cache.AddOrUpdate(
                    key: id,
                    addValueFactory: (id) =>
                    {
                        return Clone(resource);
                    },
                    updateValueFactory: (key, oldresource) =>
                    {
                        return Clone(resource);
                    });
            }
        }

        /// <inheritdoc/>
        private V1APIResource Clone(V1APIResource resource)
        {
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            return KubernetesJson.Deserialize<V1APIResource>(KubernetesJson.Serialize(resource));
        }
    }
}

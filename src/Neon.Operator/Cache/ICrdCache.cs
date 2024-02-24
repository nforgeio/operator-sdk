//-----------------------------------------------------------------------------
// FILE:	    ICrdCache.cs
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

using k8s;
using k8s.Models;

namespace Neon.Operator.Cache
{
    /// <summary>
    /// Describes a CRD cache.
    /// </summary>
    internal interface ICrdCache
    {
        /// <summary>
        /// Attempts to retrieve cached resource by ID.
        /// </summary>
        /// <param name="metadata">The entity attribute.</param>
        /// <returns>The retrieved CRD or <c>null</c> when it's not cached.</returns>
        V1APIResource Get(KubernetesEntityAttribute metadata);

        /// <summary>
        /// Attempts to retrieve a cached resource by type.
        /// </summary>
        /// <typeparam name="Tresource">Specifies the CRD type.</typeparam>
        /// <returns>The retrieved CRD or <c>null</c> when it's not cached.</returns>
        V1APIResource Get<TResource>()
            where TResource : IKubernetesObject<V1ObjectMeta>;

        /// <summary>
        /// Adds or replaces an resource in the cache.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="version"></param>
        /// <param name="resource">Specifies the new or updated CRD.</param>
        void Upsert(string group, string version, V1APIResource resource);

        /// <summary>
        /// Adds or replaces an resource in the cache.
        /// </summary>
        /// <param name="resourceList">Specifies the new or updated CRD.</param>
        void Upsert(V1APIResourceList resourceList);

        /// <summary>
        /// Removes an resource from the cache, if present.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="version"></param>
        /// <param name="resource">Specifies the CRD to be removed.</param>
        void Remove(string group, string version, V1APIResource resource);

        /// <summary>
        /// Clears the cache.
        /// </summary>
        void Clear();
    }
}

//-----------------------------------------------------------------------------
// FILE:	    DependentResource.cs
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

using k8s;
using k8s.Models;

namespace Neon.Operator.Attributes
{
    /// <summary>
    /// <para>
    /// Defines a dependent resource. This allows the operator to respond to updates to Dependent resources.
    /// For example, a Deployment will create a ReplicaSet and the Deployment controller may want to Reconcile
    /// when there are updates to the ReplicaSet.
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class DependentResource<TEntity> : IDependentResource
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DependentResource()
        {
        }

        /// <inheritdoc/>
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        /// <inheritdoc/>
        public KubernetesEntityAttribute GetKubernetesEntityAttribute()
        {
            return GetEntityType().GetKubernetesTypeMetadata();
        }
    }
}

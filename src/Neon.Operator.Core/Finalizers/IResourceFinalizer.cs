//-----------------------------------------------------------------------------
// FILE:	    IResourceFinalizer.cs
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

using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

namespace Neon.Operator.Finalizers
{
    /// <summary>
    /// Describes a ginalizer manager.
    /// </summary>
    /// <typeparam name="TEntity">The type of the k8s entity.</typeparam>
    [OperatorComponent(OperatorComponentType.Finalizer)]
    [ResourceFinalizer]
    public interface IResourceFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>, new()
    {
        /// <summary>
        /// Identifies the resource finalizer
        /// </summary>
        string Identifier { get; set; }

        /// <summary>
        /// Called when the entity needs to be finalized.
        /// </summary>
        /// <param name="entity">Specifies the entity being finalized.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        Task FinalizeAsync(TEntity entity);
    }
}

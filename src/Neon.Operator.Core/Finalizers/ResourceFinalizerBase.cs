//-----------------------------------------------------------------------------
// FILE:	    IResourceFinalizer.cs
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
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;
using Neon.Operator.Core;

namespace Neon.Operator.Finalizers
{
    /// <summary>
    /// Describes a ginalizer manager.
    /// </summary>
    /// <typeparam name="TEntity">The type of the k8s entity.</typeparam>
    [OperatorComponent(OperatorComponentType.Finalizer)]
    [ResourceFinalizer]
    public class ResourceFinalizerBase<TEntity> : IResourceFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>, new()
    {
        /// <summary>
        /// Identifies the resource finalizer
        /// </summary>
        public string Identifier
        {
            get
            {
                if (!string.IsNullOrEmpty(identifier))
                {
                    return identifier;
                }

                var metadata = new TEntity().GetKubernetesTypeMetadata();
                var name     = $"{metadata.Group}/{GetType().Name.ToLowerInvariant()}";

                // Trim if longer than max label length.

                if (name.Length > Constants.MaxLabelLength)
                {
                    name = name.Substring(0, Constants.MaxLabelLength);
                };

                return name;
            }
            set
            {
                if (value.Length > Constants.MaxLabelLength)
                {
                    throw new ArgumentException($"Identifier length cannot exceed {Constants.MaxLabelLength}");
                }

                identifier = value;
            }
        }

        private string identifier;

        /// <summary>
        /// Called when the entity needs to be finalized.
        /// </summary>
        /// <param name="entity">Specifies the entity being finalized.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public virtual Task FinalizeAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }
    }
}

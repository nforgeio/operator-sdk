//-----------------------------------------------------------------------------
// FILE:	    IFinalizerBuilder.cs
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

using k8s;
using k8s.Models;

namespace Neon.Operator.Finalizers
{
    /// <summary>
    /// Describes a finalizer builder.
    /// </summary>
    internal interface IFinalizerBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
        /// <param name="serviceProvider">Specifies the dependency injection service provider.</param>
        /// <returns></returns>
        public IEnumerable<IResourceFinalizer<TEntity>> BuildFinalizers<TEntity>(IServiceProvider serviceProvider)
            where TEntity : IKubernetesObject<V1ObjectMeta>, new();

        /// <summary>
        /// Builds the finalizer.
        /// </summary>
        /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
        /// <typeparam name="TFinalizer">Specifies the finalizer type.</typeparam>
        /// <param name="serviceProvider">Specifies the dependency injection service provider.</param>
        /// <returns>The resource finalizer.</returns>
        IResourceFinalizer<TEntity> BuildFinalizer<TEntity, TFinalizer>(IServiceProvider serviceProvider)
            where TEntity : IKubernetesObject<V1ObjectMeta>, new();
    }
}

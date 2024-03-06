//-----------------------------------------------------------------------------
// FILE:	    IResourceController.cs
// CONTRIBUTOR: Jeff Lill, Marcus Bowyer
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
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

namespace Neon.Operator.Controllers
{
    /// <summary>
    /// Describes the interface used to implement Neon based operator controllers.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the Kubernetes entity being managed.</typeparam>
    [OperatorComponent(ComponentType = OperatorComponentType.Controller)]
    [ResourceController]
    public interface IResourceController<TEntity> : IResourceController
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// The lease name for the controller resource manager.
        /// </summary>
        string LeaseName { get; set; }

        /// <summary>
        /// Called when a new resource is detected or when the non-status part of an existing resource
        /// is modified.
        /// </summary>
        /// <param name="entity">The new or modified resource.</param>
        /// <returns>
        /// A <see cref="ResourceControllerResult"/> indicating the the current event or possibly a new event is 
        /// to be requeue with a possible delay.  <c>null</c> may also bne returned, indicating that
        /// the event is not to be requeued.
        /// </returns>
        public Task<ResourceControllerResult> ReconcileAsync(TEntity entity);

        /// <summary>
        /// Called when the status part of a resource has been modified.
        /// </summary>
        /// <param name="entity">The modified resource.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public Task StatusModifiedAsync(TEntity entity);

        /// <summary>
        /// Called when a resource has been deleted.
        /// </summary>
        /// <param name="entity">The deleted resource.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public Task DeletedAsync(TEntity entity);

        /// <summary>
        /// Called when the instance has a Leader Elector and this instance has
        /// assumed leadership.
        /// </summary>
        public Task OnPromotionAsync();

        /// <summary>
        /// Called when the instance has a Leader Elector this instance has
        /// been demoted.
        /// </summary>
        public Task OnDemotionAsync();

        /// <summary>
        /// Called when the instance has a Leader Elector and a new leader has
        /// been elected.
        /// </summary>
        /// <param name="identity">Identifies the new leader.</param>
        public Task OnNewLeaderAsync(string identity);

        /// <summary>
        /// Called when an exception is thrown. This allows the operator to define the retry policy.
        /// </summary>
        /// <param name="entity">Specifies the type of the entity.</param>
        /// <param name="attempt">Specifies the number of times the operation has been attempted.</param>
        /// <param name="exception">Specifies the exception.</param>
        /// <returns>The <see cref="ErrorPolicyResult"/>.</returns>
        public Task<ErrorPolicyResult> ErrorPolicyAsync(TEntity entity, int attempt, Exception exception);
    }
}

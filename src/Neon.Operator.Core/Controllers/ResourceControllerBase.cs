//-----------------------------------------------------------------------------
// FILE:	    ResourceControllerBase.cs
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
using System.Threading;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

namespace Neon.Operator.Controllers
{
    /// <summary>
    /// Base resource controller class.
    /// </summary>
    [OperatorComponent(ComponentType = OperatorComponentType.Controller)]
    public class ResourceControllerBase<T> : IResourceController<T>
        where T : IKubernetesObject<V1ObjectMeta>
    {
        private string leaseName;

        /// <inheritdoc/>
        public string FieldSelector { get; set; } = null;

        /// <inheritdoc/>
        public string LabelSelector { get; set; } = null;

        /// <inheritdoc/>
        public string LeaseName
        {
            get
            {
                if (!string.IsNullOrEmpty(leaseName))
                {
                    return leaseName;
                }

                return $"{GetType().Name}.{typeof(T).GetKubernetesTypeMetadata().PluralName}".ToLower();
            }
            set
            {
                leaseName = value;
            }
        }

        /// <inheritdoc/>
        public bool IsLeader { get; set; }

        /// <inheritdoc/>
        public virtual Task DeletedAsync(T entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task<ErrorPolicyResult> ErrorPolicyAsync(T entity, int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ErrorPolicyResult());
        }

        /// <inheritdoc/>
        public virtual Task OnPromotionAsync(CancellationToken cancellationToken = default)
        {
            IsLeader = true;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task OnDemotionAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task OnNewLeaderAsync(string identity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task<ResourceControllerResult> ReconcileAsync(T entity, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ResourceControllerResult>(null);
        }

        /// <inheritdoc/>
        public virtual Task StartAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task StatusModifiedAsync(T entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns <see cref="ResourceControllerResult.Ok()"/>
        /// </summary>
        /// <returns></returns>
        public ResourceControllerResult Ok() => null;

        /// <summary>
        /// Returns <see cref="ResourceControllerResult.RequeueEvent(TimeSpan)"/>
        /// </summary>
        /// <returns></returns>
        public ResourceControllerResult RequeueEvent(TimeSpan delay) => ResourceControllerResult.RequeueEvent(delay);

        /// <summary>
        /// Returns <see cref="ResourceControllerResult.RequeueEvent(TimeSpan, WatchEventType)"/>
        /// </summary>
        /// <returns></returns>
        public ResourceControllerResult RequeueEvent(TimeSpan delay, WatchEventType eventType) => ResourceControllerResult.RequeueEvent(delay, eventType);
    }
}

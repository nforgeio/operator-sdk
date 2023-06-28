//-----------------------------------------------------------------------------
// FILE:	    IAdmissionWebhook.cs
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

using System.Threading.Tasks;

using k8s;
using k8s.Models;

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// Represents an Admission webhook.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
    /// <typeparam name="TResult">Specifies the result type.</typeparam>
    public interface IAdmissionWebhook<TEntity, TResult>
        where TEntity : IKubernetesObject<V1ObjectMeta>, new()
        where TResult : AdmissionResult, new()
    {
        /// <summary>
        /// The number of seconds to apply to timeouts when using Dev Tunnels.
        /// </summary>
        public int DevTimeoutSeconds { get; }

        /// <summary>
        /// Returns the webhook name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the webhook endpoint.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Returns the webhook endpoint.
        /// </summary>
        public WebhookType WebhookType { get; }

        /// <summary>
        /// The namespace selector.
        /// </summary>
        V1LabelSelector NamespaceSelector { get; set; }

        /// <summary>
        /// The Object selector.
        /// </summary>
        V1LabelSelector ObjectSelector { get; set; }

        /// <summary>
        /// Operation for <see cref="AdmissionOperations.Create"/>.
        /// </summary>
        /// <param name="newEntity">The newly created entity that should be validated.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A result that is transmitted to Kubernetes.</returns>
        public TResult Create(TEntity newEntity, bool dryRun);

        /// <inheritdoc cref="Create"/>
        public Task<TResult> CreateAsync(TEntity newEntity, bool dryRun);

        /// <summary>
        /// Operation for <see cref="AdmissionOperations.Update"/>.
        /// </summary>
        /// <param name="oldEntity">The old entity. This is the "old" version before the update.</param>
        /// <param name="newEntity">The new entity. This is the "new" version after the update is performed.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A result that is transmitted to Kubernetes.</returns>
        public TResult Update(TEntity oldEntity, TEntity newEntity, bool dryRun);

        /// <inheritdoc cref="Update"/>
        public Task<TResult> UpdateAsync(TEntity oldEntity, TEntity newEntity, bool dryRun);

        /// <summary>
        /// Operation for <see cref="AdmissionOperations.Delete"/>.
        /// </summary>
        /// <param name="oldEntity">The entity that is being deleted.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A result that is transmitted to Kubernetes.</returns>
        public TResult Delete(TEntity oldEntity, bool dryRun);

        /// <inheritdoc cref="Delete"/>
        public Task<TResult> DeleteAsync(TEntity oldEntity, bool dryRun);

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <param name="result"></param>
        /// <param name="request"></param>
        /// <returns>The <see cref="AdmissionResponse"/>.</returns>
        public AdmissionResponse TransformResult(TResult result, AdmissionRequest<TEntity> request);
    }
}

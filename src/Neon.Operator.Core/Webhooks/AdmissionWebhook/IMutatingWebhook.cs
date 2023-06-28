//-----------------------------------------------------------------------------
// FILE:	    IMutatingWebhook.cs
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
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Logging;

using Neon.Operator.Attributes;

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// Describes a mutating webhook.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
    [OperatorComponent(OperatorComponentType.MutationWebhook)]
    [ValidatingWebhook]
    public interface IMutatingWebhook<TEntity> : IAdmissionWebhook<TEntity, MutationResult>
        where TEntity : IKubernetesObject<V1ObjectMeta>, new()
    {
        /// <summary>
        /// Json patch formatter.
        /// </summary>
        JsonPatchDeltaFormatter Formatter { get; }

        /// <summary>
        /// The webhook configuration.
        /// </summary>
        V1MutatingWebhookConfiguration WebhookConfiguration(
            OperatorSettings operatorSettings,
            bool useTunnel = false,
            string tunnelUrl = null,
            ILogger<IMutatingWebhook<TEntity>> logger = null);

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        Task CreateAsync(IServiceProvider serviceProvider);
    }
}

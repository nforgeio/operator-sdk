//-----------------------------------------------------------------------------
// FILE:	    IMutatingWebhook.cs
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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using k8s;
using k8s.Autorest;
using k8s.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neon.Common;
using Neon.Diagnostics;
using Neon.Operator.Attributes;

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// Describes a mutating webhook.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
    [OperatorComponent(OperatorComponentType.MutationWebhook)]
    [ValidatingWebhook]
    public class MutatingWebhookBase<TEntity> : IMutatingWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>, new()
    {
        /// <inheritdoc/>
        private static JsonPatchDeltaFormatter formatter = new JsonPatchDeltaFormatter();
        /// <inheritdoc/>
        public JsonPatchDeltaFormatter Formatter => formatter;

        /// <inheritdoc/>
        public V1LabelSelector NamespaceSelector { get; set; } = null;

        /// <inheritdoc/>
        public V1LabelSelector ObjectSelector { get; set; } = null;

        /// <inheritdoc />
        string IAdmissionWebhook<TEntity, MutationResult>.Endpoint
        {
            get => WebhookHelper.CreateEndpoint<TEntity>(this.GetType(), WebhookType);
        }

        /// <inheritdoc/>
        public int DevTimeoutSeconds => 30;
        /// <inheritdoc/>
        public string Name => $"{GetType().Namespace ?? "root"}.{typeof(TEntity).Name}.{GetType().Name}".ToLowerInvariant();

        /// <inheritdoc/>
        public WebhookType WebhookType => WebhookType.Mutating;


        /// <inheritdoc/>
        public virtual AdmissionResponse TransformResult(MutationResult result, AdmissionRequest<TEntity> request)
        {
            var response = new AdmissionResponse
            {
                Allowed  = result.Valid,
                Status   = new AdmissionResponse.Reason { Code = result.StatusCode ?? 0, Message = result.StatusMessage, },
                Warnings = result.Warnings.ToArray(),
            };

            if (result.ModifiedObject != null)
            {
                response.PatchType = AdmissionResponse.JsonPatch;

                var node1 = JsonNode.Parse(KubernetesJson.Serialize(
                    request.Operation == "DELETE"
                        ? request.OldObject
                        : request.Object));

                var node2 = JsonNode.Parse(KubernetesJson.Serialize(result.ModifiedObject));
                var diff  = node1.Diff(node2, Formatter);

                response.Patch     = Convert.ToBase64String(Encoding.UTF8.GetBytes(KubernetesJson.Serialize(diff)));
                response.PatchType = AdmissionResponse.JsonPatch;
            }

            return response;
        }
     
        /// <inheritdoc/>
        public virtual V1MutatingWebhookConfiguration WebhookConfiguration(
            OperatorSettings operatorSettings,
            bool useTunnel = false,
            string tunnelUrl = null,
            ILogger<IMutatingWebhook<TEntity>> logger = null)
        {
            var hook = this.GetType().GetCustomAttribute<WebhookAttribute>();

            var clientConfig = new Admissionregistrationv1WebhookClientConfig()
            {
                Service = new Admissionregistrationv1ServiceReference()
                {
                    Name              = operatorSettings.Name,
                    NamespaceProperty = operatorSettings.PodNamespace,
                    Path              = WebhookHelper.CreateEndpoint<TEntity>(this.GetType(), WebhookType.Mutating)
                }
            };

            if (useTunnel && !string.IsNullOrEmpty(tunnelUrl))
            {
                logger?.LogDebugEx(() => $"Configuring Webhook {this.GetType().Name} to use Dev Tunnel.");

                clientConfig.Service = null;
                clientConfig.CaBundle = null;
                clientConfig.Url = tunnelUrl.TrimEnd('/') + WebhookHelper.CreateEndpoint<TEntity>(this.GetType(), WebhookType.Mutating);
            }

            var webhookConfig = new V1MutatingWebhookConfiguration().Initialize();
            webhookConfig.Metadata.Name = hook.Name;

            if (!useTunnel && operatorSettings.certManagerEnabled)
            {
                logger?.LogDebugEx(() => $"Not using tunnel for Webhook {this.GetType().Name}.");

                webhookConfig.Metadata.Annotations = webhookConfig.Metadata.EnsureAnnotations();
                webhookConfig.Metadata.Annotations.Add("cert-manager.io/inject-ca-from", $"{operatorSettings.PodNamespace}/{operatorSettings.Name}");
            }

            webhookConfig.Webhooks = new List<V1MutatingWebhook>()
            {
                new V1MutatingWebhook()
                {
                    Name                    = hook.Name,
                    Rules                   = new List<V1RuleWithOperations>(),
                    ClientConfig            = clientConfig,
                    AdmissionReviewVersions = hook.AdmissionReviewVersions,
                    FailurePolicy           = hook.FailurePolicy,
                    SideEffects             = hook.SideEffects,
                    TimeoutSeconds          = useTunnel ? DevTimeoutSeconds : hook.TimeoutSeconds,
                    NamespaceSelector       = NamespaceSelector,
                    MatchPolicy             = hook.MatchPolicy,
                    ObjectSelector          = ObjectSelector,
                    ReinvocationPolicy      = hook.ReinvocationPolicy
                }
            };

            var rules = this.GetType().GetCustomAttributes<WebhookRuleAttribute>();

            foreach (var rule in rules)
            {
                webhookConfig.Webhooks.FirstOrDefault().Rules.Add(
                    new V1RuleWithOperations()
                    {
                        ApiGroups   = rule.ApiGroups,
                        ApiVersions = rule.ApiVersions,
                        Operations  = rule.Operations.ToList(),
                        Resources   = rule.Resources,
                        Scope       = rule.Scope
                    }
                );
            }

            return webhookConfig;
        }

        
        /// <inheritdoc/>
        public virtual async Task CreateAsync(IServiceProvider serviceProvider)
        {
            var operatorSettings   = serviceProvider.GetRequiredService<OperatorSettings>();
            var certManagerOptions = serviceProvider.GetService<CertManagerOptions>();
            var k8s                = serviceProvider.GetService<IBasicKubernetes>();
            var logger             = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<IMutatingWebhook<TEntity>>();

            logger?.LogInformationEx(() => $"Checking for webhook {this.GetType().Name}.");

            bool useDevTunnel      = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VS_TUNNEL_URL"));
            string certificateName = operatorSettings.certManagerEnabled ? operatorSettings.Name : null;
            var webhookConfig      = WebhookConfiguration(
                                            operatorSettings: operatorSettings,
                                            useTunnel: NeonHelper.IsDevWorkstation,
                                            tunnelUrl: Environment.GetEnvironmentVariable("VS_TUNNEL_URL"),
                                            logger: logger);

            try
            {
                var webhook = await k8s.AdmissionregistrationV1.ReadMutatingWebhookConfigurationAsync(webhookConfig.Name());

                webhook.Webhooks = webhookConfig.Webhooks;
                webhook.Metadata.Annotations = webhookConfig.Metadata.Annotations;
                webhook.Metadata.Labels = webhookConfig.Metadata.Labels;

                await k8s.AdmissionregistrationV1.ReplaceMutatingWebhookConfigurationAsync(webhook, webhook.Name());

                logger?.LogInformationEx(() => $"Webhook {this.GetType().Name} updated.");
            }
            catch (HttpOperationException e)
            {
                logger?.LogInformationEx(() => $"Webhook {this.GetType().Name} not found, creating.");

                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await k8s.AdmissionregistrationV1.CreateMutatingWebhookConfigurationAsync(webhookConfig);

                    logger?.LogInformationEx(() => $"Webhook {this.GetType().Name} created.");
                }
                else
                {
                    logger?.LogErrorEx(e);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public virtual MutationResult Create(TEntity newEntity, bool dryRun)
        {
            return MutationResult.NoChanges();
        }

        /// <inheritdoc/>
        public async virtual Task<MutationResult> CreateAsync(TEntity newEntity, bool dryRun)
        {
            return await Task.FromResult(MutationResult.NoChanges());
        }

        /// <inheritdoc/>
        public virtual MutationResult Update(TEntity oldEntity, TEntity newEntity, bool dryRun)
        {
            return MutationResult.NoChanges();
        }

        /// <inheritdoc/>
        public async virtual Task<MutationResult> UpdateAsync(TEntity oldEntity, TEntity newEntity, bool dryRun)
        {
            return await Task.FromResult(MutationResult.NoChanges());
        }

        /// <inheritdoc/>
        public virtual MutationResult Delete(TEntity oldEntity, bool dryRun)
        {
            return MutationResult.NoChanges();
        }

        /// <inheritdoc/>
        public async virtual Task<MutationResult> DeleteAsync(TEntity oldEntity, bool dryRun)
        {
            return await Task.FromResult(MutationResult.NoChanges());
        }

        /// <inheritdoc cref="MutationResult.NoChanges(int, string)"/>
        public MutationResult NoChanges(int statusCode = StatusCodes.Status200OK, string statusMessage = null)
        {
            return MutationResult.NoChanges(statusCode, statusMessage);
        }

        /// <inheritdoc cref="MutationResult.Modified(object, string[])"/>
        public MutationResult Modified(object modifiedEntry, params string[] warnings)
        {
            return MutationResult.Modified(modifiedEntry, warnings);
        }

        /// <inheritdoc />
        public string GetEndpoint()
        {
            return WebhookHelper.CreateEndpoint<TEntity>(this.GetType(), WebhookType);
        }
    }
}

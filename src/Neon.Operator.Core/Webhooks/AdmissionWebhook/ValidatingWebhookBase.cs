//-----------------------------------------------------------------------------
// FILE:	    IValidatingWebhook.cs
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
    /// Describes a Validating webhook.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
    [OperatorComponent(ComponentType = OperatorComponentType.ValidationWebhook)]
    [ValidatingWebhook]
    public class ValidatingWebhookBase<TEntity> : IValidatingWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>, new()
    {
        /// <summary>
        /// The webhook configuration.
        /// </summary>
        public V1ValidatingWebhookConfiguration WebhookConfiguration(
            OperatorSettings                     operatorSettings,
            bool                                 useTunnel = false, 
            string                               tunnelUrl = null,
            ILogger<IValidatingWebhook<TEntity>> logger    = null)
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

                clientConfig.Service  = null;
                clientConfig.CaBundle = null;
                clientConfig.Url      = tunnelUrl.TrimEnd('/') + WebhookHelper.CreateEndpoint<TEntity>(this.GetType(), WebhookType.Validating);
            }

            var webhookConfig = new V1ValidatingWebhookConfiguration().Initialize();
            webhookConfig.Metadata.Name = hook.Name;

            if (!useTunnel && operatorSettings.certManagerEnabled)
            {
                logger?.LogDebugEx(() => $"Not using tunnel for Webhook {this.GetType().Name}.");

                webhookConfig.Metadata.Annotations = webhookConfig.Metadata.EnsureAnnotations();

                webhookConfig.Metadata.Annotations.Add("cert-manager.io/inject-ca-from", $"{operatorSettings.PodNamespace}/{operatorSettings.Name}");
            }

            webhookConfig.Webhooks = new List<V1ValidatingWebhook>()
            {
                new V1ValidatingWebhook()
                {
                    Name                    = hook.Name,
                    Rules                   = new List<V1RuleWithOperations>(),
                    ClientConfig            = clientConfig,
                    AdmissionReviewVersions = hook.AdmissionReviewVersions,
                    FailurePolicy           = hook.FailurePolicy.ToMemberString(),
                    SideEffects             = hook.SideEffects.ToMemberString(),
                    TimeoutSeconds          = useTunnel ? DevTimeoutSeconds : hook.TimeoutSeconds,
                    NamespaceSelector       = NamespaceSelector,
                    MatchPolicy             = hook.MatchPolicy.ToMemberString(),
                    ObjectSelector          = ObjectSelector,
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
        public V1LabelSelector NamespaceSelector { get; set; } = null;

        /// <inheritdoc/>

        public V1LabelSelector ObjectSelector { get; set; } = null;

        /// <inheritdoc />
        string IAdmissionWebhook<TEntity, ValidationResult>.Endpoint => WebhookHelper.CreateEndpoint<TEntity>(this.GetType(), WebhookType);

        /// <inheritdoc/>
        WebhookType WebhookType => WebhookType.Validating;

        /// <inheritdoc/>
        public int DevTimeoutSeconds => 30;

        /// <inheritdoc/>
        public string Name => $"{GetType().Namespace ?? "root"}.{typeof(TEntity).Name}.{GetType().Name}".ToLowerInvariant();


        /// <inheritdoc/>
        WebhookType IAdmissionWebhook<TEntity, ValidationResult>.WebhookType => throw new NotImplementedException();

        /// <inheritdoc/>
        public async Task CreateAsync(IServiceProvider serviceProvider)
        {
            var operatorSettings   = serviceProvider.GetRequiredService<OperatorSettings>();
            var certManagerOptions = serviceProvider.GetService<CertManagerOptions>();
            var k8s                = serviceProvider.GetService<IBasicKubernetes>();
            var logger             = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<IValidatingWebhook<TEntity>>();

            logger?.LogInformationEx(() => $"Checking for webhook {this.GetType().Name}.");

            bool useDevTunnel      = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VS_TUNNEL_URL"));
            string certificateName = operatorSettings.certManagerEnabled ? operatorSettings.Name : null;
            var webhookConfig      = WebhookConfiguration(
                operatorSettings: operatorSettings,
                useTunnel:        NeonHelper.IsDevWorkstation,
                tunnelUrl:        Environment.GetEnvironmentVariable("VS_TUNNEL_URL"),
                logger:           logger);

            try
            {
                var webhook = await k8s.AdmissionregistrationV1.ReadValidatingWebhookConfigurationAsync(webhookConfig.Name());

                webhook.Webhooks             = webhookConfig.Webhooks;
                webhook.Metadata.Annotations = webhookConfig.Metadata.Annotations;
                webhook.Metadata.Labels      = webhookConfig.Metadata.Labels;

                await k8s.AdmissionregistrationV1.ReplaceValidatingWebhookConfigurationAsync(webhook, webhook.Name());

                logger?.LogInformationEx(() => $"Webhook {this.GetType().Name} updated.");
            }
            catch (HttpOperationException e) 
            {
                logger?.LogInformationEx(() => $"Webhook {this.GetType().Name} not found, creating.");

                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound) 
                {
                    await k8s.AdmissionregistrationV1.CreateValidatingWebhookConfigurationAsync(webhookConfig);

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
        public virtual ValidationResult Create(TEntity newEntity, bool dryRun)
        {
            return ValidationResult.Success();
        }

        /// <inheritdoc/>
        public virtual async Task<ValidationResult> CreateAsync(TEntity newEntity, bool dryRun)
        {
            return await Task.FromResult(ValidationResult.Success());
        }

        /// <inheritdoc/>
        public virtual ValidationResult Update(TEntity oldEntity, TEntity newEntity, bool dryRun)
        {
            return ValidationResult.Success();
        }

        /// <inheritdoc/>
        public virtual async Task<ValidationResult> UpdateAsync(TEntity oldEntity, TEntity newEntity, bool dryRun)
        {
            return await Task.FromResult(ValidationResult.Success());
        }

        /// <inheritdoc/>
        public virtual ValidationResult Delete(TEntity oldEntity, bool dryRun)
        {
            return ValidationResult.Success();
        }

        /// <inheritdoc/>
        public virtual async Task<ValidationResult> DeleteAsync(TEntity oldEntity, bool dryRun)
        {
            return await Task.FromResult(ValidationResult.Success());
        }

        /// <inheritdoc/>
        public virtual AdmissionResponse TransformResult(ValidationResult result, AdmissionRequest<TEntity> request)
        {
            var response = new AdmissionResponse
            {
                Allowed  = result.Valid,
                Warnings = result.Warnings.ToArray(),
                Status   = result.StatusMessage == null
                    ? null
                    : new AdmissionResponse.Reason { Code = result.StatusCode ?? 0, Message = result.StatusMessage, }
            };

            return response;
        }

        /// <inheritdoc cref="ValidationResult.Success(string[])"/>
        public ValidationResult Success(params string[] warnings)
        {
            return ValidationResult.Success(warnings);
        }

        /// <inheritdoc cref="ValidationResult.Fail(int?, string)"/>
        public ValidationResult Fail(int? statusCode = null, string statusMessage = null)
        {
            return ValidationResult.Fail(statusCode, statusMessage);
        }

        /// <inheritdoc />
        public string GetEndpoint()
        {
            return WebhookHelper.CreateEndpoint<TEntity>(this.GetType(), WebhookType);
        }
    }
}

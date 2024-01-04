//-----------------------------------------------------------------------------
// FILE:	    IOperatorBuilder.cs
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

using k8s;
using k8s.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Neon.K8s;
using Neon.Operator.Controllers;
using Neon.Operator.Finalizers;
using Neon.Operator.ResourceManager;
using Neon.Operator.Webhooks;

namespace Neon.Operator
{
    /// <summary>
    /// Operator  builder interface.
    /// </summary>
    public interface IOperatorBuilder
    {
        /// <summary>
        /// Returns the original service collection.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// <para>
        /// Adds a CRD controller to the operator.
        /// </para>
        /// </summary>
        /// <typeparam name="TImplementation">The type of the controller to register.</typeparam>
        /// <typeparam name="TEntity">The type of the entity to associate the controller with.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IOperatorBuilder AddController<TImplementation, TEntity>(
            string                  @namespace = null,
            ResourceManagerOptions  options = null,
            LeaderElectionConfig    leaderConfig = null,
            bool                    leaderElectionDisabled = false)

            where TImplementation : class, IResourceController<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new();

        /// <summary>
        /// <para>
        /// Adds a CRD finalizer to the operator.
        /// </para>
        /// </summary>
        /// <typeparam name="TImplementation">The type of the finalizer to register.</typeparam>
        /// <typeparam name="TEntity">The type of the entity to associate the finalizer with.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IOperatorBuilder AddFinalizer<TImplementation, TEntity>()
            where TImplementation : class, IResourceFinalizer<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new();

        /// <summary>
        /// <para>
        /// Adds a mutating webhook to the operator.
        /// </para>
        /// </summary>
        /// <typeparam name="TImplementation">The type of the webhook to register.</typeparam>
        /// <typeparam name="TEntity">The type of the entity to associate the webhook with.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IOperatorBuilder AddMutatingWebhook<TImplementation, TEntity>()
            where TImplementation : class, IMutatingWebhook<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new();

        /// <summary>
        /// <para>
        /// Adds a validating webhook to the operator.
        /// </para>
        /// </summary>
        /// <typeparam name="TImplementation">The type of the webhook to register.</typeparam>
        /// <typeparam name="TEntity">The type of the entity to associate the webhook with.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IOperatorBuilder AddValidatingWebhook<TImplementation, TEntity>()
            where TImplementation : class, IValidatingWebhook<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new();

        /// <summary>
        /// <para>
        /// For development purposes only. Adds a tunnel and configures webhooks to 
        /// tunnel through to the developer workstation.
        /// </para>
        /// </summary>
        /// <param name="hostname">The hostname for the tunnel.</param>
        /// <param name="port">The port.</param>
        /// <param name="ngrokDirectory">The directory where the ngrok binary is located.</param>
        /// <param name="ngrokAuthToken">The ngrok auth token</param>
        /// <param name="enabled">Set to false to optionally disable this feature.</param>
        /// <returns>The <see cref="IOperatorBuilder"/>.</returns>
        IOperatorBuilder AddNgrokTunnnel(
            string      hostname       = "localhost",
            int         port           = 5000,
            string      ngrokDirectory = null,
            string      ngrokAuthToken = null,
            bool        enabled        = true);

        /// <summary>
        /// Add a startup check to the operator.
        /// </summary>
        /// <typeparam name="THealthChecker">
        /// Specifies the type handling the health check.  Note that this
        /// must implement <see cref="IHealthCheck"/>.
        /// </typeparam>
        /// <param name="name">Optionally specifies the health checker's name.</param>
        /// <returns>The <see cref="IOperatorBuilder"/>.</returns>
        IOperatorBuilder AddStartupCheck<THealthChecker>(string name = null)
            where THealthChecker : class, IHealthCheck;

        /// <summary>
        /// Add a liveness check to the operator.
        /// </summary>
        /// <typeparam name="THealthChecker">
        /// Specifies the type handling the health check.  Note that this
        /// must implement <see cref="IHealthCheck"/>.
        /// </typeparam>
        /// <param name="name">Optionally specifies the health checker's name.</param>
        /// <returns>The <see cref="IOperatorBuilder"/>.</returns>
        IOperatorBuilder AddLivenessCheck<THealthChecker>(string name = null)
            where THealthChecker : class, IHealthCheck;

        /// <summary>
        /// Add a readiness check to the operator.
        /// </summary>
        /// <typeparam name="THealthChecker">
        /// Specifies the type handling the health check.  Note that this
        /// must implement <see cref="IHealthCheck"/>.
        /// </typeparam>
        /// <param name="name">Optionally specifies the health checker's name.</param>
        /// <returns>The <see cref="IOperatorBuilder"/>.</returns>
        IOperatorBuilder AddReadinessCheck<THealthChecker>(string name = null)
            where THealthChecker : class, IHealthCheck;
    }
}

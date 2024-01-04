//-----------------------------------------------------------------------------
// FILE:	    ComponentRegister.cs
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

using Neon.Operator.Controllers;
using Neon.Operator.Finalizers;
using Neon.Operator.Webhooks;

namespace Neon.Operator.Builder
{
    /// <summary>
    /// $todo(marcusbooyah): documentation
    /// </summary>
    public class ComponentRegister
    {
        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public ComponentRegister()
        {
            ControllerRegistrations        = new HashSet<ControllerRegistration>();
            EntityRegistrations            = new HashSet<EntityRegistration>();
            FinalizerRegistrations         = new HashSet<FinalizerRegistration>();
            MutatingWebhookRegistrations   = new HashSet<MutatingWebhookRegistration>();
            ResourceManagerRegistrations   = new HashSet<Type>();
            ValidatingWebhookRegistrations = new HashSet<ValidatingWebhookRegistration>();
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public HashSet<ControllerRegistration> ControllerRegistrations { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public HashSet<EntityRegistration> EntityRegistrations { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public HashSet<FinalizerRegistration> FinalizerRegistrations { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public HashSet<MutatingWebhookRegistration> MutatingWebhookRegistrations { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public HashSet<Type> ResourceManagerRegistrations { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public HashSet<ValidatingWebhookRegistration> ValidatingWebhookRegistrations { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <typeparam name="TController"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        public void RegisterController<TController, TEntity>()
            where TController : class, IResourceController<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new()
        {
            ControllerRegistrations.Add(new ControllerRegistration(typeof(TController), typeof(TEntity)));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public void RegisterController(Type controllerType, Type entityType)
        {
            ControllerRegistrations.Add(new ControllerRegistration(controllerType, entityType));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        public void RegisterEntity<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>, new()
        {
            EntityRegistrations.Add(new EntityRegistration(typeof(TEntity)));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public void RegisterEntity(Type entityType)
        {
            EntityRegistrations.Add(new EntityRegistration(entityType));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <typeparam name="TFinalizer"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        public void RegisterFinalizer<TFinalizer, TEntity>()
            where TFinalizer : class, IResourceFinalizer<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new()
        {
            FinalizerRegistrations.Add(new FinalizerRegistration(typeof(TFinalizer), typeof(TEntity)));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public void RegisterFinalizer(Type finalizerType, Type entityType)
        {
            FinalizerRegistrations.Add(new FinalizerRegistration(finalizerType, entityType));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <typeparam name="TMutator"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        public void RegisterMutatingWebhook<TMutator, TEntity>()
            where TMutator : class, IMutatingWebhook<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new()
        {
            MutatingWebhookRegistrations.Add(new MutatingWebhookRegistration(typeof(TMutator), typeof(TEntity)));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public void RegisterMutatingWebhook(Type webhookType, Type entityType)
        {
            MutatingWebhookRegistrations.Add(new MutatingWebhookRegistration(webhookType, entityType));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <typeparam name="TResourceManager"></typeparam>
        public void RegisterResourceManager<TResourceManager>()
        {
            ResourceManagerRegistrations.Add(typeof(TResourceManager));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public void RegisterResourceManager(Type resourceManagerType)
        {
            ResourceManagerRegistrations.Add(resourceManagerType);

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        /// <typeparam name="TMutator"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        public void RegisterValidatingWebhook<TMutator, TEntity>()
            where TMutator : class, IValidatingWebhook<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta>, new()
        {
            ValidatingWebhookRegistrations.Add(new ValidatingWebhookRegistration(typeof(TMutator), typeof(TEntity)));

            return;
        }

        /// <summary>
        /// $todo(marcusbooyah): documentation
        /// </summary>
        public void RegisterValidatingWebhook(Type webhookType, Type entityType)
        {
            ValidatingWebhookRegistrations.Add(new ValidatingWebhookRegistration(webhookType, entityType));

            return;
        }
    }

    /// <summary>
    /// $todo(marcusbooyah): documentation
    /// </summary>
    /// <param name="ControllerType"></param>
    /// <param name="EntityType"></param>
    public record ControllerRegistration(Type ControllerType, Type EntityType);

    /// <summary>
    /// $todo(marcusbooyah): documentation
    /// </summary>
    /// <param name="EntityType"></param>
    public record EntityRegistration(Type EntityType);

    /// <summary>
    /// $todo(marcusbooyah): documentation
    /// </summary>
    /// <param name="FinalizerType"></param>
    /// <param name="EntityType"></param>
    public record FinalizerRegistration(Type FinalizerType, Type EntityType);

    /// <summary>
    /// $todo(marcusbooyah): documentation
    /// </summary>
    /// <param name="WebhookType"></param>
    /// <param name="EntityType"></param>
    public record MutatingWebhookRegistration(Type WebhookType, Type EntityType);

    /// <summary>
    /// $todo(marcusbooyah): documentation
    /// </summary>
    /// <param name="ResourceManagerType"></param>
    public record ResourceManagerRegistration(Type ResourceManagerType);

    /// <summary>
    /// $todo(marcusbooyah): documentation
    /// </summary>
    /// <param name="WebhookType"></param>
    /// <param name="EntityType"></param>
    public record ValidatingWebhookRegistration(Type WebhookType, Type EntityType);
}

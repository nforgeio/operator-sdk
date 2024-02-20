//-----------------------------------------------------------------------------
// FILE:	    ResourceManager.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using AsyncKeyedLock;

using k8s;
using k8s.Autorest;
using k8s.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neon.Common;
using Neon.Diagnostics;
using Neon.K8s;
using Neon.Operator.Cache;
using Neon.Operator.Controllers;
using Neon.Operator.Entities;
using Neon.Operator.EventQueue;
using Neon.Operator.Finalizers;
using Neon.Operator.Util;
using Neon.Tasks;

using Prometheus;

using WatchEventType = Neon.Operator.Controllers.WatchEventType;

namespace Neon.Operator.ResourceManager
{
    /// <summary>
    /// Used by custom Kubernetes operators to manage a collection of custom resources.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the custom Kubernetes entity type being managed.</typeparam>
    /// <typeparam name="TController">Specifies the entity controller type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class helps makes it easier to manage custom cluster resources.  Simply construct an
    /// instance with <see cref="ResourceManager{TResource, TController}"/> in your controller 
    /// (passing any custom settings as parameters) and then call <see cref="StartAsync()"/>.
    /// </para>
    /// <para>
    /// After the resource manager starts, your controller's <see cref="IResourceController{TEntity}.ReconcileAsync(TEntity)"/>, 
    /// <see cref="IResourceController{TEntity}.DeletedAsync(TEntity)"/>, and <see cref="IResourceController{TEntity}.StatusModifiedAsync(TEntity)"/> 
    /// methods will be called as related resource related events are received.
    /// </para>
    /// <para>
    /// Your handlers should perform any necessary operations to converge the actual state with set
    /// of resources passed and then return a <see cref="ResourceControllerResult"/> to control event 
    /// requeuing or <c>null</c>.
    /// </para>
    /// <note>
    /// For most operators, we recommend that all of your handlers execute shared code that handles
    /// all reconcilation by comparing the desired state represented by the custom resources passed to
    /// your handler in the dictionary passed with the current state and then performing any required 
    /// converge operations as opposed to handling just resource add/edits for reconciled events or
    /// just resource deletions for deletred events.  This is often cleaner by keeping all of your
    /// reconcilation logic in one place.
    /// </note>
    /// <para><b>OPERATOR LIFECYCLE</b></para>
    /// <para>
    /// Kubernetes operators work by watching cluster resources via the API server.  The Operator SDK
    /// starts watching the resource specified by <typeparamref name="TEntity"/> and raises the
    /// controller events as they are received, handling any failures seamlessly.  The <see cref="ResourceManager{TResource, TController}"/> 
    /// class helps keep track of the existing resources as well reducing the complexity of determining why
    /// an event was raised. Operator SDK also periodically raises reconciled events even when nothing has 
    /// changed.  This appears to happen once a minute.
    /// </para>
    /// <para>
    /// When your operator first starts, a reconciled event will be raised for each custom resource of 
    /// type <typeparamref name="TEntity"/> in the cluster and the resource manager will add
    /// these resources to its internal dictionary.  By default, the resource manager will not call 
    /// your handler until all existing resources have been added to this dictionary.  Then after the 
    /// resource manager has determined that it has collected all of the existing resources, it will call 
    /// your handler for the first time, passing a <c>null</c> resource name and your handler can start
    /// doing it's thing.
    /// </para>
    /// <note>
    /// <para>
    /// Holding back calls to your reconciled handler is important in many situations by ensuring
    /// that the entire set of resources is known before the first handler call.  Without this,
    /// your handler may perform delete actions on resources that exist in the cluster but haven't
    /// been reconciled yet which could easily cause a lot of trouble, especially if your operator
    /// gets scheduled and needs to start from scratch.
    /// </para>
    /// </note>
    /// <para>
    /// After the resource manager has all of the resources, it will start calling your reconciled
    /// handler for every event raised by the operator and start calling your deleted and status modified
    /// handlers for changes.
    /// </para>
    /// <para>
    /// Your handlers are called <b>after</b> the internal resource dictionary is updated with
    /// changes implied by the event.  This means that a new resource received with a reconcile
    /// event will be added to the dictionary before your handler is called and a resource from
    /// a deleted event will be removed before the handler is called.
    /// </para>
    /// <para>
    /// The name of the new, deleted, or changed resource will be passed to your handler.  This
    /// will be passed as <c>null</c> when nothing changed.
    /// </para>
    /// <para><b>LEADER LEADER ELECTION</b></para>
    /// <para>
    /// It's often necessary to ensure that only one entity (typically a pod) is managing a specific
    /// resource kind at a time.  For example, let's say you're writing an operator that manages the
    /// deployment of other applications based on custom resources.  In this case, it'll be important
    /// that only a single operator instance be managing the application at a time to avoid having the 
    /// operators step on each other's toes when the operator has multiple replicas running.
    /// </para>
    /// <para>
    /// The Operator SDK and other operator SDKs allow operators to indicate that only a single
    /// replica in the cluster should be allowed to process changes to custom resources.  This uses
    /// Kubernetes leases and works well for simple operators that manage only a single resource or 
    /// perhaps a handful of resources that are not also managed by other operators.
    /// </para>
    /// <para>
    /// It's often handy to be able to have an operator application manage multiple resources, with
    /// each resource kind having their own lease enforcing this exclusivity:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Allow multiple replicas of an operator be able to load balance processing of different 
    /// resource kinds.
    /// </item>
    /// <item>
    /// Allow operators to consolidate processing of different resource kinds, some that need
    /// exclusivity and others that don't.  This can help reduce the number of operator applications
    /// that need to be created, deployed, and managed and can also reduce the number of system
    /// processes required along with their associated overhead.
    /// </item>
    /// </list>
    /// </remarks>
    public sealed class ResourceManager<TEntity, TController> : IResourceManager, IDisposable
        where TEntity : IKubernetesObject<V1ObjectMeta>, new()
        where TController : IResourceController<TEntity>
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ResourceManager()
        {
            kubernetesTypes = Assembly.GetAssembly(typeof(V1Pod)).DefinedTypes.Where(t => t.GetCustomAttribute<KubernetesEntityAttribute>() != null).Select(t => t.GetKubernetesCrdName());
        }

        //---------------------------------------------------------------------
        // Instance members

        private bool                                                     isDisposed = false;
        private bool                                                     started    = false;
        private static IEnumerable<string>                               kubernetesTypes;
        private ResourceManagerOptions                                   options;
        private OperatorSettings                                         operatorSettings;
        private ResourceManagerMetrics<TEntity, TController>             metrics;
        private IKubernetes                                              k8s;
        private IServiceProvider                                         serviceProvider;
        private IResourceCache<TEntity, TEntity>                         resourceCache;
        private IResourceCache<TEntity, IKubernetesObject<V1ObjectMeta>> dependentResourceCache;
        private ICrdCache                                                crdCache;
        private IFinalizerManager<TEntity>                               finalizerManager;
        private AsyncKeyedLocker<string>                                 lockProvider;
        private List<string>                                             resourceNamespaces;
        private Type                                                     controllerType;
        private ILogger<ResourceManager<TEntity, TController>>           logger;
        private ILoggerFactory                                           loggerFactory;
        private LeaderElectionConfig                                     leaderConfig;
        private bool                                                     leaderElectionDisabled;
        private LeaderElector                                            leaderElector;
        private Task                                                     leaderTask;
        private Task                                                     watcherTask;
        private CancellationTokenSource                                  watcherTcs;
        private EventQueue<TEntity, TController>                         eventQueue;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options">
        /// Optionally specifies options that customize the resource manager's behavior.  Reasonable
        /// defaults will be used when this isn't specified.
        /// </param>
        /// <param name="leaderConfig">
        /// Optionally specifies the <see cref="LeaderElectionConfig"/> to be used to control
        /// whether only a single entity is managing a specific resource kind at a time.  See
        /// the <b>LEADER ELECTION SECTION</b> in the <see cref="ResourceManager{TResource, TController}"/>
        /// remarks for more information.
        /// </param>
        /// <param name="leaderElectionDisabled">Optionally specifies the leader election should be disabled.</param>
        /// <param name="serviceProvider">Specifies the depedency injection service provider.</param>
        public ResourceManager(
            IServiceProvider        serviceProvider,
            ResourceManagerOptions  options                = null,
            LeaderElectionConfig    leaderConfig           = null,
            bool                    leaderElectionDisabled = false)
        {
            Covenant.Requires<ArgumentNullException>(serviceProvider != null, nameof(ServiceProvider));
            Covenant.Requires<ArgumentException>(options.WatchNamespace == null || options.WatchNamespace != string.Empty, nameof(options.WatchNamespace));
            
            this.serviceProvider        = serviceProvider;
            this.options                = options ?? serviceProvider.GetRequiredService<ResourceManagerOptions>();
            this.operatorSettings       = serviceProvider.GetRequiredService<OperatorSettings>();
            this.leaderConfig           = leaderConfig;
            this.leaderElectionDisabled = leaderElectionDisabled;
            this.metrics                = serviceProvider.GetRequiredService<ResourceManagerMetrics<TEntity, TController>>();
            this.loggerFactory          = serviceProvider.GetService<ILoggerFactory>();
            this.logger                 = loggerFactory?.CreateLogger<ResourceManager<TEntity, TController>>();

            if (options.WatchNamespace != null)
            {
                this.resourceNamespaces = Regex.Replace(options.WatchNamespace, @"\s+", "").Split(',').ToList();
            }

            this.options.Validate();

            this.controllerType = typeof(TController);
            var entityType      = typeof(TEntity);
        }

        /// <inheritdoc/>
        public ResourceManagerOptions Options()
        {
            return options;
        }

        /// <summary>
        /// Starts the resource manager.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the resource manager has already been started.</exception>
        public async Task StartAsync()
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(serviceProvider != null, nameof(serviceProvider));
            Covenant.Requires<InvalidOperationException>(!started, $"[{nameof(ResourceManager<TEntity, TController>)}] is already running.");

            this.k8s                    = serviceProvider.GetRequiredService<IKubernetes>();
            this.resourceCache          = serviceProvider.GetRequiredService<IResourceCache<TEntity, TEntity>>();
            this.dependentResourceCache = serviceProvider.GetRequiredService<IResourceCache<TEntity, IKubernetesObject<V1ObjectMeta>>>();
            this.crdCache               = serviceProvider.GetRequiredService<ICrdCache>();
            this.finalizerManager       = serviceProvider.GetRequiredService<IFinalizerManager<TEntity>>();
            this.lockProvider           = serviceProvider.GetRequiredService<AsyncKeyedLocker<string>>();

            IResourceController<TEntity> controller;
            
            controller = CreateController(serviceProvider);

            await controller.StartAsync(serviceProvider);

            options.FieldSelector = options.FieldSelector ?? controller.FieldSelector;
            options.LabelSelector = options.LabelSelector ?? controller.LabelSelector;

            if (leaderConfig == null && !leaderElectionDisabled)
            {
                this.leaderConfig =
                    new LeaderElectionConfig(
                        @namespace: operatorSettings.PodNamespace,
                        leaseName:  controller.LeaseName,
                        identity:   Pod.Name);
            }

            if (started)
            {
                throw new InvalidOperationException($"[{nameof(ResourceManager<TEntity, TController>)}] is already running.");
            }

            //-----------------------------------------------------------------
            // Start the leader elector if enabled.


            // Start the leader elector when enabled.

            ResetLeaderElector();

            await Task.CompletedTask;
        }

        private void ResetLeaderElector()
        {
            if (leaderConfig != null)
            {
                leaderElector = new LeaderElector(
                    k8s,
                    leaderConfig,
                    onStartedLeading: OnPromotion,
                    onStoppedLeading: OnDemotion,
                    onNewLeader:      OnNewLeader);

                leaderTask = leaderElector.RunAsync();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            if (leaderElector != null)
            {
                leaderElector.Dispose();

                try
                {
                    leaderTask.WaitWithoutAggregate();
                }
                catch (OperationCanceledException)
                {
                    // We're expecting this.
                }

                leaderElector = null;
                leaderTask    = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Ensures that the instance has not been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
        private void EnsureNotDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException($"ResourceManager[{typeof(TEntity).FullName}]");
            }
        }

        /// <summary>
        /// Ensures that the controller has been started before the operator.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when Operator is running before <see cref="StartAsync()"/> is called for this controller.
        /// </exception>
        private void EnsureStarted()
        {
            if (!started)
            {
                throw new InvalidOperationException($"You must call [{nameof(TController)}.{nameof(StartAsync)}()] first.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task EnsurePermissionsAsync()
        {
            await SyncContext.Clear;

            using var activity = TraceContext.ActivitySource?.StartActivity();

            logger?.LogInformationEx(() => $"Checking permissions for {typeof(TEntity)}.");

            HttpOperationResponse<object> response;

            try
            {
                if (resourceNamespaces == null)
                {
                    response = await k8s.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync<TEntity>(
                        allowWatchBookmarks: true,
                        watch: true);

                    response.Dispose();
                }
                else
                {
                    foreach (var @namespace in resourceNamespaces)
                    {
                        response = await k8s.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync<TEntity>(
                            @namespace,
                            allowWatchBookmarks: true,
                            watch:               true);

                        response.Dispose();
                    }
                }
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Forbidden)
            {
                logger?.LogErrorEx(e, () => $"Cannot watch type {typeof(TEntity)}, please check RBAC rules for the controller.");
                throw;
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e, () => $"Cannot watch type {typeof(TEntity)}.");

                throw;
            }

            logger?.LogInformationEx(() => $"Permissions for {typeof(TEntity)} ok.");

            if (options.DependentResources != null)
            {
                logger?.LogInformationEx(() => $"Checking permissions for dependent resources.");

                foreach (var dependent in options.DependentResources.Where(d => !kubernetesTypes.Contains(d.GetEntityType().GetKubernetesCrdName())))
                {
                    try
                    {
                        logger?.LogInformationEx(() => $"Checking permissions for {dependent.GetEntityType()}.");

                        if (resourceNamespaces == null)
                        {
                            response = await k8s.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                                dependent.GetKubernetesEntityAttribute().Group,
                                dependent.GetKubernetesEntityAttribute().ApiVersion,
                                dependent.GetKubernetesEntityAttribute().PluralName,
                                allowWatchBookmarks: true,
                                watch: true);

                            response.Dispose();
                        }
                        else
                        {
                            foreach (var @namespace in resourceNamespaces)
                            {
                                response = await k8s.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                                    dependent.GetKubernetesEntityAttribute().Group,
                                    dependent.GetKubernetesEntityAttribute().ApiVersion,
                                    @namespace,
                                    dependent.GetKubernetesEntityAttribute().PluralName,
                                    allowWatchBookmarks: true,
                                    watch: true);

                                response.Dispose();
                            }
                        }
                    }
                    catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        logger?.LogErrorEx(e, () => $"Cannot watch type {dependent.GetEntityType()}, please check RBAC rules for the controller.");
                        throw;
                    }
                    catch (Exception e)
                    {
                        logger?.LogErrorEx(e, () => $"Cannot watch type {typeof(TEntity)}.");

                        throw;
                    }

                    logger?.LogInformationEx(() => $"Permissions for {dependent.GetEntityType()} ok.");
                }
            }
        }

        /// <summary>
        /// Starts the CRD watchers.
        /// </summary>
        /// <param name="cancellationToken">Specifies the cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task StartCrdWatchersAsync(CancellationToken cancellationToken)
        {
            await SyncContext.Clear;

            using var activity = TraceContext.ActivitySource?.StartActivity();

            var crdMeta = typeof(TEntity).GetKubernetesTypeMetadata();
            var crdName = typeof(TEntity).GetKubernetesCrdName();

            if (kubernetesTypes.Contains(crdName))
            {
                return;
            }

            _ = k8s.WatchAsync<V1CustomResourceDefinition>(
                async (@event) =>
                {
                    await SyncContext.Clear;

                    crdCache.Upsert(@event.Value);

                    logger?.LogInformationEx(() => $"Updated {typeof(TEntity)} CRD.");
                },
                fieldSelector:     $"metadata.name={crdName}",
                retryDelay:        operatorSettings.WatchRetryDelay,
                cancellationToken: cancellationToken,
                logger:            logger);

            crdCache.Upsert(await k8s.ApiextensionsV1.ReadCustomResourceDefinitionAsync(crdName));

            if (options.DependentResources != null)
            {
                foreach (var dependent in options.DependentResources.Where(d => !kubernetesTypes.Contains(d.GetEntityType().GetKubernetesCrdName())))
                {
                    crdName = dependent.GetEntityType().GetKubernetesCrdName();

                    _ = k8s.WatchAsync<V1CustomResourceDefinition>(
                        async (@event) =>
                        {
                            crdCache.Upsert(@event.Value);
                            logger?.LogInformationEx(() => $"Updated {dependent.GetEntityType()} CRD.");
                            await Task.CompletedTask;
                        },
                        fieldSelector:     $"metadata.name={crdName}",
                        retryDelay:        operatorSettings.WatchRetryDelay,
                        cancellationToken: cancellationToken,
                        logger:            logger);

                    crdCache.Upsert(await k8s.ApiextensionsV1.ReadCustomResourceDefinitionAsync(crdName));
                }
            }
        }

        /// <summary>
        /// Called when the instance has a <see cref="LeaderElector"/> and this instance has
        /// assumed leadership.
        /// </summary>
        private void OnPromotion()
        {
            try
            {
                logger?.LogInformationEx(() => $"{typeof(TController)}[{typeof(TEntity)}] PROMOTED");

                IsLeader = true;

                Task.Run(
                    async () =>
                    {
                        await SyncContext.Clear;

                        if (options.ManageCustomResourceDefinitions)
                        {
                            await CreateOrReplaceCustomResourceDefinitionAsync();
                        }

                        watcherTcs = new CancellationTokenSource();

                        await EnsurePermissionsAsync();
                        await StartCrdWatchersAsync(watcherTcs.Token);

                        // Start the watcher.

                        watcherTask = WatchAsync(watcherTcs.Token);

                        // Inform the controller.

                        await using (var scope = serviceProvider.CreateAsyncScope())
                        {
                            await CreateController(scope.ServiceProvider).OnPromotionAsync();
                        }
                    }).Wait();
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
                Thread.Sleep(leaderConfig.LeaseDuration);
                ResetLeaderElector();
            }
        }

        /// <summary>
        /// Called when the instance has a <see cref="LeaderElector"/> this instance has
        /// been demoted.
        /// </summary>
        private void OnDemotion()
        {
            logger?.LogInformationEx(() => $"{typeof(TController)}[{typeof(TEntity)}] DEMOTED");

            IsLeader = false;

            try
            {
                Task.Run(
                    async () =>
                    {
                        // Stop the watcher.

                        watcherTcs.Cancel();

                        await watcherTask;

                        // Inform the controller.

                        await using (var scope = serviceProvider.CreateAsyncScope())
                        {
                            await CreateController(scope.ServiceProvider).OnDemotionAsync();
                        }

                    }).Wait();
            }
            finally
            {
                // Reset operator state.

                watcherTask  = null;
            }
        }

        /// <summary>
        /// Called when the instance has a <see cref="LeaderElector"/> and a new leader has
        /// been elected.
        /// </summary>
        /// <param name="identity">Identifies the new leader.</param>
        private void OnNewLeader(string identity)
        {
            LeaderIdentity = identity;

            try
            {
                Task.Run(
                    async () =>
                    {
                        logger?.LogInformationEx(() => $"{typeof(TController)}[{typeof(TEntity)}] LEADER-IS: {identity}");

                        // Inform the controller.

                        await using (var scope = serviceProvider.CreateAsyncScope())
                        {
                            await CreateController(scope.ServiceProvider).OnNewLeaderAsync(identity);
                        }

                    }).Wait();
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
            }
        }

        /// <summary>
        /// Creates or updates CRDs for the controller.
        /// </summary>
        /// <returns></returns>
        private async Task CreateOrReplaceCustomResourceDefinitionAsync()
        {
            await SyncContext.Clear;

            using var activity = TraceContext.ActivitySource?.StartActivity();

            try
            {
                var generator    = serviceProvider.GetRequiredService<CustomResourceGenerator>();
                var crd          = generator.GenerateCustomResourceDefinition(typeof(TEntity));

                logger?.LogInformationEx(() => $"Checking CustomResourceDefinition [{crd.Name()}]");

                var existingList = await k8s.ApiextensionsV1.ListCustomResourceDefinitionAsync(fieldSelector: $"metadata.name={crd.Name()}");

                var existingCustomResourceDefinition = existingList?.Items?.SingleOrDefault();

                if (existingCustomResourceDefinition != null)
                {
                    logger?.LogInformationEx(() => $"Updating CustomResourceDefinition [{crd.Name()}]");

                    crd.Metadata.ResourceVersion = existingCustomResourceDefinition.ResourceVersion();

                    await k8s.ApiextensionsV1.ReplaceCustomResourceDefinitionAsync(crd, crd.Name());
                }
                else
                {
                    logger?.LogInformationEx(() => $"Creating CustomResourceDefinition [{crd.Name()}]");

                    await k8s.ApiextensionsV1.CreateCustomResourceDefinitionAsync(crd);
                }

                await k8s.ApiextensionsV1.WaitForCustomResourceDefinitionAsync<TEntity>();
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
            }
        }

        /// <summary>
        /// Returns <c>true</c> when this instance is currently the leader for the resource type.
        /// </summary>
        public bool IsLeader { get; private set; }

        /// <summary>
        /// Returns the identity of the current leader for the resource type or <c>null</c>
        /// when there is no leader.
        /// </summary>
        public string LeaderIdentity { get; private set; }

        /// <summary>
        /// Creates a controller instance.
        /// </summary>
        /// <returns>The controller.</returns>
        private IResourceController<TEntity> CreateController(IServiceProvider serviceProvider)
        {
            return (IResourceController<TEntity>)ActivatorUtilities.CreateInstance(serviceProvider, controllerType);
        }

        /// <summary>
        /// Temporarily implements our own resource watcher.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to stop the watcher when the operator is demoted.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task WatchAsync(CancellationToken cancellationToken)
        {
            await SyncContext.Clear;

            //-----------------------------------------------------------------
            // We're going to use this dictionary to keep track of the [Status]
            // property of the resources we're watching so we can distinguish
            // between changes to the status vs. changes to anything else in
            // the resource.
            //
            // The dictionary simply holds the status property serialized to
            // JSON, with these keyed by resource name.  Note that the resource
            // entities might not have a [Status] property.

            var entityType   = typeof(TEntity);
            var statusGetter = entityType.GetProperty("Status")?.GetMethod;
            
            //-----------------------------------------------------------------
            // Our watcher handler action.

            var actionAsync =
                async (WatchEvent<TEntity> @event) =>
                {
                    using (var activity = TraceContext.ActivitySource?.StartActivity("ActionAsync"))
                    {
                        var result            = (ResourceControllerResult)null;
                        var modifiedEventType = ModifiedEventType.Other;
                        var resource          = @event.Value;
                        var resourceName      = resource.Metadata.Name;

                        using (await lockProvider.LockAsync(@event.Value.Uid(), cancellationToken).ConfigureAwait(false))
                        {
                            try
                            {
                                await using (var scope = serviceProvider.CreateAsyncScope())
                                {
                                    var cachedEntity = resourceCache.Upsert(resource, out modifiedEventType);

                                    if (@event.Force)
                                    {
                                        logger?.LogDebugEx(() => $"FORCING UPDATE. Event type [{modifiedEventType}] on resource [{resource.Kind}/{resource.Namespace()}/{resourceName}]");

                                        modifiedEventType = ModifiedEventType.Other;
                                    }

                                    if (modifiedEventType == ModifiedEventType.Finalizing)
                                    {
                                        @event.Type = (k8s.WatchEventType)WatchEventType.Modified;
                                    }

                                    switch (@event.Type)
                                    {
                                        case (k8s.WatchEventType)WatchEventType.Added:

                                            try
                                            {
                                                metrics.ReconcileEventsTotal?.Inc();

                                                if (options.AutoRegisterFinalizers)
                                                {
                                                    logger?.LogInformationEx(() => $"Registering finalizers for resource [{resource.Kind}/{resource.Namespace()}/{resourceName}]");

                                                    await finalizerManager.RegisterAllFinalizersAsync(resource);
                                                }

                                                using (metrics.ReconcileTimeSeconds.NewTimer())
                                                {
                                                    result = await CreateController(scope.ServiceProvider).ReconcileAsync(resource);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                metrics.ReconcileErrorsTotal.Inc();
                                                logger?.LogErrorEx(() => $"Event type [{@event.Type}] on resource [{resource.Kind}/{resourceName}] threw a [{e.GetType()}] error. Attempt [{@event.Attempt}]");

                                                var errorPolicyResult = await CreateController(scope.ServiceProvider).ErrorPolicyAsync(resource, @event.Attempt, e);

                                                if (errorPolicyResult.Requeue)
                                                {
                                                    @event.Attempt += 1;

                                                    resourceCache.Remove(resource);

                                                    await eventQueue.RequeueAsync(
                                                        @event, 
                                                        delay:          errorPolicyResult.RequeueDelay, 
                                                        watchEventType: (k8s.WatchEventType?)errorPolicyResult.EventType);

                                                    return;
                                                }
                                            }

                                            break;

                                        case (k8s.WatchEventType)WatchEventType.Deleted:

                                            try
                                            {
                                                metrics.DeleteEventsTotal?.Inc();

                                                using (metrics.DeleteTimeSeconds.NewTimer())
                                                {
                                                    await CreateController(scope.ServiceProvider).DeletedAsync(resource);
                                                }

                                                resourceCache.Remove(resource);
                                            }
                                            catch (Exception e)
                                            {
                                                metrics.DeleteErrorsTotal?.Inc();
                                                logger?.LogErrorEx(e);
                                            }

                                            break;

                                        case (k8s.WatchEventType)WatchEventType.Modified:

                                            switch (modifiedEventType)
                                            {
                                                case ModifiedEventType.Other:

                                                    try
                                                    {
                                                        metrics.ReconcileEventsTotal?.Inc();

                                                        using (metrics.ReconcileTimeSeconds.NewTimer())
                                                        {
                                                            result = await CreateController(scope.ServiceProvider).ReconcileAsync(resource);
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        metrics.ReconcileErrorsTotal?.Inc();
                                                        logger?.LogErrorEx(e, () => $"Event type [{modifiedEventType}] on resource [{resource.Kind}/{resourceName}] threw a [{e.GetType()}] error. Attempt [{@event.Attempt}]");

                                                        var errorPolicyResult = await CreateController(scope.ServiceProvider).ErrorPolicyAsync(resource, @event.Attempt, e);

                                                        if (errorPolicyResult.Requeue)
                                                        {
                                                            @event.Attempt += 1;

                                                            resourceCache.Remove(resource);

                                                            await eventQueue.RequeueAsync(
                                                                @event,
                                                                delay:          errorPolicyResult.RequeueDelay,
                                                                watchEventType: (k8s.WatchEventType?)errorPolicyResult.EventType);

                                                            return;
                                                        }
                                                    }
                                                    break;

                                                case ModifiedEventType.Finalizing:

                                                    try
                                                    {
                                                        metrics.FinalizeTotal?.Inc();

                                                        if (!resourceCache.IsFinalizing(resource))
                                                        {
                                                            resourceCache.AddFinalizer(resource);

                                                            using (metrics.FinalizeTimeSeconds.NewTimer())
                                                            {
                                                                await finalizerManager.FinalizeAsync(resource, scope);
                                                            }
                                                        }

                                                        resourceCache.RemoveFinalizer(resource);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        metrics.FinalizeErrorsTotal?.Inc();
                                                        logger?.LogErrorEx(e);

                                                        resourceCache.RemoveFinalizer(resource);
                                                        logger?.LogErrorEx(e, () => $"Event type [{modifiedEventType}] on resource [{resource.Kind}/{resourceName}] error [attempt={@event.Attempt}]");

                                                        var errorPolicyResult = await CreateController(scope.ServiceProvider).ErrorPolicyAsync(resource, @event.Attempt, e);

                                                        if (errorPolicyResult.Requeue)
                                                        {
                                                            @event.Attempt++;

                                                            resourceCache.Remove(resource);

                                                            await eventQueue.RequeueAsync(
                                                                @event,
                                                                delay:          errorPolicyResult.RequeueDelay,
                                                                watchEventType: (k8s.WatchEventType?)errorPolicyResult.EventType);

                                                            return;
                                                        }
                                                    }
                                                    break;

                                                case ModifiedEventType.StatusUpdate:

                                                    if (statusGetter == null)
                                                    {
                                                        return;
                                                    }

                                                    var newStatus     = statusGetter.Invoke(resource, Array.Empty<object>());
                                                    var newStatusJson = newStatus == null ? null : JsonSerializer.Serialize(newStatus);
                                                    var oldStatus     = statusGetter.Invoke(cachedEntity, Array.Empty<object>());
                                                    var oldStatusJson = oldStatus == null ? null : JsonSerializer.Serialize(oldStatus);

                                                    if (newStatusJson != oldStatusJson)
                                                    {
                                                        logger?.LogDebugEx(() => $"Status updated on resource [{resource.Kind}/{resourceName}]");

                                                        try
                                                        {
                                                            metrics.StatusModifiedTotal?.Inc();

                                                            using (metrics.StatusModifiedTimeSeconds.NewTimer())
                                                            {
                                                                await CreateController(scope.ServiceProvider).StatusModifiedAsync(resource);
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            metrics.StatusModifiedErrorsTotal?.Inc();
                                                            logger?.LogErrorEx(e);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        logger?.LogDebugEx(() => $"Status not updated/no changes on resource [{resource.Kind}/{resourceName}].");
                                                    }

                                                    break;

                                                case ModifiedEventType.FinalizerUpdate:
                                                case ModifiedEventType.NoChanges:
                                                default:

                                                    logger?.LogDebugEx(() => $"Event is {modifiedEventType}. No action needed.");

                                                    break;
                                            }

                                            break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                logger?.LogCriticalEx(e);
                                logger?.LogCriticalEx("Cannot recover from exception within watch loop.  Terminating process.");
                                Environment.Exit(1);
                            }
                        }

                        if (@event.Type < (k8s.WatchEventType)WatchEventType.Deleted
                            && modifiedEventType == ModifiedEventType.Other)
                        {
                            switch (result)
                            {
                                case null:

                                    logger?.LogInformationEx(() => $@"Event type [{@event.Type}] on resource [{resource.Kind}/{resourceName}] successfully reconciled. Requeue not requested.");
                                    await eventQueue.DequeueAsync(@event);

                                    return;
                                
                                case RequeueEventResult requeue:

                                    var specificQueueTypeRequested = requeue.EventType.HasValue;
                                    var requestedQueueType = requeue.EventType ?? WatchEventType.Modified;

                                    if (specificQueueTypeRequested)
                                    {
                                        logger?.LogInformationEx(() => $@"Event type [{@event.Type}] on resource [{resource.Kind}/{resourceName}] successfully reconciled. Requeue requested as type [{requestedQueueType}] with delay [{requeue}].");
                                    }
                                    else
                                    {
                                        logger?.LogInformationEx(() => $@"Event type [{@event.Type}] on resource [{resource.Kind}/{resourceName}] successfully reconciled. Requeue requested with delay [{requeue}].");
                                    }

                                    resourceCache.Remove(resource);
                                    await eventQueue.RequeueAsync(@event, requeue.RequeueDelay, (k8s.WatchEventType?)requestedQueueType);
                                    break;
                            }
                        }
                    }
                };

            var enqueueAsync =
                async (WatchEvent<TEntity> @event) =>
                {
                    using (var activity = TraceContext.ActivitySource?.StartActivity("EnqueueResourceEvent", ActivityKind.Server))
                    {
                        var resource     = @event.Value;
                        var resourceName = resource.Metadata.Name;

                        logger?.LogDebugEx(() => $"Resource {resource.Kind} {resource.Namespace()}/{resource.Name()} received {@event.Type} event.");

                        resourceCache.Compare(resource, out var modifiedEventType);

                        @event.ModifiedEventType = modifiedEventType;

                        switch (@event.Type)
                        {
                            case (k8s.WatchEventType)WatchEventType.Added:
                            case (k8s.WatchEventType)WatchEventType.Deleted:
                            case (k8s.WatchEventType)WatchEventType.Modified:

                                await eventQueue.DequeueAsync(@event);
                                await eventQueue.EnqueueAsync(@event);
                                break;

                            case (k8s.WatchEventType)WatchEventType.Bookmark:

                                break;  // We don't care about these.

                            case (k8s.WatchEventType)WatchEventType.Error:

                                // I believe we're only going to see this for extreme scenarios, like:
                                //
                                //      1. The CRD we're watching was deleted and recreated.
                                //      2. The watcher is so far behind that part of the
                                //         history is no longer available.
                                //
                                // We're going to log this and terminate the application, expecting
                                // that Kubernetes will reschedule it so we can start over.

                                var stub = new TEntity();

                                if (!string.IsNullOrEmpty(resource.Namespace()))
                                {
                                    logger?.LogCriticalEx(() => $"Critical error watching: [namespace={resource.Namespace()}] {stub.ApiGroupAndVersion}/{stub.Kind}");
                                }
                                else
                                {
                                    logger?.LogCriticalEx(() => $"Critical error watching: {stub.ApiGroupAndVersion}/{stub.Kind}");
                                }

                                logger?.LogCriticalEx("Terminating the pod so Kubernetes can reschedule it and we can restart the watch.");
                                Environment.Exit(1);
                                break;

                            default:
                                break;
                        }
                    }
                };

            var enqueueDependentAsync =
                async (dynamic @event) =>
                {
                    using (var activity = TraceContext.ActivitySource?.StartActivity("EnqueueDependentResourceEvent", ActivityKind.Server))
                    {
                        var resource     = (IKubernetesObject<V1ObjectMeta>)@event.Value;
                        var resourceName = resource.Metadata.Name;

                        if (resource.Metadata.OwnerReferences == null)
                        {
                            return;
                        }

                        dependentResourceCache.Compare(resource, out var modifiedEventType);

                        if (resource.Metadata.OwnerReferences.Any(r => resourceCache.TryGet(r.Uid, out _)))
                        {
                            dependentResourceCache.Upsert(resource);
                        }

                        logger?.LogDebugEx(() => $"Dependent resource {resource.Kind} {resource.Namespace()}/{resource.Name()} received {@event.Type}/{modifiedEventType} event.");

                        switch (@event.Type)
                        {
                            case WatchEventType.Deleted:

                                foreach (var ownerRef in resource.Metadata.OwnerReferences)
                                {
                                    if (resourceCache.TryGet(ownerRef.Uid, out TEntity owner))
                                    {
                                        logger?.LogDebugEx(() => $"Dependent resource {resource.Kind} {resource.Namespace()}/{resource.Name()} queuing new event for {typeof(TEntity)} {owner.Namespace()}/{owner.Name()}.");

                                        var newWatchEvent = new WatchEvent<TEntity>((k8s.WatchEventType)WatchEventType.Modified, owner, force: true);

                                        await eventQueue.DequeueAsync(newWatchEvent);
                                        await eventQueue.EnqueueAsync(newWatchEvent);
                                    }
                                }

                                break;

                            case WatchEventType.Modified:

                                if (modifiedEventType == ModifiedEventType.Other)
                                {
                                    foreach (var ownerRef in resource.Metadata.OwnerReferences)
                                    {
                                        if (resourceCache.TryGet(ownerRef.Uid, out TEntity owner))
                                        {
                                            logger?.LogDebugEx(() => $"Dependent resource {resource.Kind} {resource.Namespace()}/{resource.Name()} queuing new event for {typeof(TEntity)} {owner.Namespace()}/{owner.Name()}.");

                                            var newWatchEvent = new WatchEvent<TEntity>((k8s.WatchEventType)WatchEventType.Modified, owner, force: true);

                                            await eventQueue.DequeueAsync(newWatchEvent);
                                            await eventQueue.EnqueueAsync(newWatchEvent);
                                        }
                                    }
                                }

                                break;

                            case WatchEventType.Bookmark:

                                break;  // We don't care about these.

                            case WatchEventType.Error:

                                // I believe we're only going to see this for extreme scenarios, like:
                                //
                                //      1. The CRD we're watching was deleted and recreated.
                                //      2. The watcher is so far behind that part of the
                                //         history is no longer available.
                                //
                                // We're going to log this and terminate the application, expecting
                                // that Kubernetes will reschedule it so we can start over.

                                var stub = new TEntity();

                                if (!string.IsNullOrEmpty(resource.Namespace()))
                                {
                                    logger?.LogCriticalEx(() => $"Critical error watching: [namespace={resource.Namespace()}] {stub.ApiGroupAndVersion}/{stub.Kind}");
                                }
                                else
                                {
                                    logger?.LogCriticalEx(() => $"Critical error watching: {stub.ApiGroupAndVersion}/{stub.Kind}");
                                }

                                logger?.LogCriticalEx("Terminating the pod so Kubernetes can reschedule it and we can restart the watch.");
                                Environment.Exit(1);
                                break;

                            default:
                                break;
                        }
                    }
                };

            this.eventQueue = new EventQueue<TEntity, TController>(
                k8s:           k8s, 
                options:       options, 
                eventHandler:  actionAsync, 
                metrics:       serviceProvider.GetRequiredService<EventQueueMetrics<TEntity, TController>>(),
                loggerFactory: loggerFactory);

            //-----------------------------------------------------------------
            // Start the watcher.

            try
            {
                var tasks = new List<Task>();

                if (this.resourceNamespaces != null && crdCache.Get(typeof(TEntity).GetKubernetesCrdName())?.Spec.Scope != "Cluster")
                {
                    foreach (var ns in resourceNamespaces)
                    {
                        tasks.Add(k8s.WatchAsync<TEntity>(
                            actionAsync:        enqueueAsync, 
                            namespaceParameter: ns, 
                            fieldSelector:      options.FieldSelector,
                            labelSelector:      options.LabelSelector,
                            retryDelay:         operatorSettings.WatchRetryDelay,
                            cancellationToken:  cancellationToken,
                            logger:             logger));
                    }
                }
                else
                {
                    tasks.Add(k8s.WatchAsync<TEntity>(
                        actionAsync:       enqueueAsync,
                        fieldSelector:     options.FieldSelector,
                        labelSelector:     options.LabelSelector,
                        retryDelay:        operatorSettings.WatchRetryDelay,
                        cancellationToken: cancellationToken,
                        logger:            logger));
                }

                foreach (var dependent in options.DependentResources)
                {
                    var watchMethod = typeof(KubernetesExtensions).GetMethod("WatchAsync").MakeGenericMethod(dependent.GetEntityType());
                    var args        = new object[watchMethod.GetParameters().Count()];

                    args[0] = k8s;
                    args[1] = enqueueDependentAsync;
                    args[3] = options.FieldSelector;
                    args[4] = options.LabelSelector;
                    args[8] = operatorSettings.WatchRetryDelay;
                    args[9] = cancellationToken;
                    args[10] = logger;

                    if (this.resourceNamespaces != null && crdCache.Get(dependent.GetEntityType().GetKubernetesCrdName())?.Spec.Scope != "Cluster")
                    {
                        foreach (var @namespace in this.resourceNamespaces)
                        {
                            args[2] = @namespace;

                            tasks.Add((Task)watchMethod.Invoke(k8s, args));
                        }
                    }
                    else
                    {
                        tasks.Add((Task)watchMethod.Invoke(k8s, args));
                    }
                }

                await NeonHelper.WaitAllAsync(tasks, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // This is thrown when the watcher is stopped due the operator being demoted.

                return;
            }
            catch (HttpOperationException)
            {
                return;
            }
        }
    }
}

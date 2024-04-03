//-----------------------------------------------------------------------------
// FILE:	    EventQueue.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Logging;

using Neon.Diagnostics;
using Neon.K8s;
using Neon.Operator.EventQueue;
using Neon.Tasks;

using Prometheus;

namespace Neon.Operator.ResourceManager
{
    /// <summary>
    /// Implements operator event queues.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
    /// <typeparam name="TController">Specifies the controller type.</typeparam>
    internal class EventQueue<TEntity, TController>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly IKubernetes                                                            k8s;
        private readonly ILogger<EventQueue<TEntity, TController>>                              logger;
        private readonly ResourceManagerOptions                                                 options;
        private readonly ConcurrentDictionary<WatchEvent<TEntity>, CancellationTokenSource>     queue;
        private readonly ConcurrentDictionary<string, DateTime>                                 currentEvents;
        private readonly EventQueueMetrics<TEntity, TController>                                metrics;
        private readonly Func<WatchEvent<TEntity>, Task>                                        eventHandler;
        private readonly Channel<string>                                                        eventChannel;
        private readonly Channel<string>                                                        finalizeChannel;
        private readonly Task[]                                                                 reconcileTasks;
        private readonly Task[]                                                                 finalizeTasks;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="k8s">Specifies the Kubernetes client.</param>
        /// <param name="metrics">Specifies the event queue metrics.</param>
        /// <param name="options">Optionally specifies custom resource manager options.</param>
        /// <param name="eventHandler">Optionally specifies a watched event handler.</param>
        /// <param name="loggerFactory">Optionally specifies the logger factory.</param>
        /// <param name="cancellationToken"></param>
        public EventQueue(
            IKubernetes                             k8s,
            ResourceManagerOptions                  options           = null,
            EventQueueMetrics<TEntity, TController> metrics           = null,
            Func<WatchEvent<TEntity>, Task>         eventHandler      = null,
            ILoggerFactory                          loggerFactory     = null,
            CancellationToken                       cancellationToken = default)
        {
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));
            Covenant.Requires<ArgumentNullException>(metrics != null, nameof(metrics));

            options ??= new ResourceManagerOptions();

            this.k8s             = k8s;
            this.options         = options;
            this.eventHandler    = eventHandler;
            this.metrics         = metrics;
            this.logger          = loggerFactory?.CreateLogger<EventQueue<TEntity, TController>>();
            this.queue           = new ConcurrentDictionary<WatchEvent<TEntity>, CancellationTokenSource>();
            this.currentEvents   = new ConcurrentDictionary<string, DateTime>();
            this.eventChannel    = Channel.CreateUnbounded<string>();
            this.finalizeChannel = Channel.CreateUnbounded<string>();
            this.reconcileTasks  = new Task[options.MaxConcurrentReconciles];
            this.finalizeTasks   = new Task[options.MaxConcurrentFinalizers];

            metrics.MaxActiveWorkers.IncTo(options.MaxConcurrentReconciles + options.MaxConcurrentFinalizers);

            Metrics.DefaultRegistry.AddBeforeCollectCallback(
                async cancel =>
                {
                    var values = currentEvents.Values.Select(v => (DateTime.UtcNow - v).TotalSeconds);

                    metrics.UnfinishedWorkSeconds.IncTo(values.Sum());

                    if (values.Count() > 0)
                    {
                        metrics.LongestRunningProcessorSeconds.IncTo(values.Max());
                    }

                    await Task.CompletedTask;
                });

            _ = StartReconcileConsumersAsync(cancellationToken);
            _ = StartFinalizerConsumersAsync(cancellationToken);
        }

        /// <summary>
        /// Starts task consumers.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task StartReconcileConsumersAsync(CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    for (int i = 0; i < reconcileTasks.Length; i++)
                    {
                        var task = reconcileTasks[i];

                        if (task == null || !task.Status.Equals(TaskStatus.Running))
                        {
                            reconcileTasks[i] = ReconcileConsumerAsync(cancellationToken);
                        }
                    }

                    await Task.WhenAny(reconcileTasks);
                }
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
                throw;
            }
        }

        /// <summary>
        /// Starts task consumers.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task StartFinalizerConsumersAsync(CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    for (int i = 0; i < finalizeTasks.Length; i++)
                    {
                        var task = finalizeTasks[i];

                        if (task == null || !task.Status.Equals(TaskStatus.Running))
                        {
                            finalizeTasks[i] = FinalizerConsumerAsync(cancellationToken);
                        }
                    }

                    await Task.WhenAny(finalizeTasks);
                }
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
                throw;
            }
        }

        /// <summary>
        /// Implements an event consumer for reconcile tasks.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task ReconcileConsumerAsync(CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            using var worker = metrics.ActiveWorkers.TrackInProgress();

            try
            {
                while (await eventChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (eventChannel.Reader.TryRead(out var uid))
                    {
                        var @event = queue.Keys.Where(key => key.Value.Uid() == uid).FirstOrDefault();

                        if (@event == null || @event.Value == null || queue[@event].IsCancellationRequested)
                        {
                            continue;
                        }

                        try
                        {
                            currentEvents.TryAdd(uid, DateTime.UtcNow);

                            metrics.QueueDurationSeconds.Observe((DateTime.UtcNow - @event.CreatedAt).TotalSeconds);
                            logger?.LogDebugEx(() => $"Executing event [{@event.Type}] for resource [{@event.Value.Kind}/{@event.Value.Name()}]");

                            using (var timer = metrics.WorkDurationSeconds.NewTimer())
                            {
                                await eventHandler?.Invoke(@event);
                            }
                        }
                        finally
                        {
                            currentEvents.Remove(uid, out _);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
                throw;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                logger?.LogInformationEx(() => "Reconcile consumer cancelled.");
            }
        }

        /// <summary>
        /// Implements an event consumer for finalizers.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task FinalizerConsumerAsync(CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            using var worker = metrics.ActiveWorkers.TrackInProgress();

            try
            {
                while (await finalizeChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (finalizeChannel.Reader.TryRead(out var uid))
                    {
                        var @event = queue.Keys.Where(key => key.Value.Uid() == uid).FirstOrDefault();

                        if (@event == null || @event.Value == null || queue[@event].IsCancellationRequested)
                        {
                            continue;
                        }

                        try
                        {
                            currentEvents.TryAdd(uid, DateTime.UtcNow);

                            metrics.QueueDurationSeconds.Observe((DateTime.UtcNow - @event.CreatedAt).TotalSeconds);
                            logger?.LogDebugEx(() => $"Executing event [{@event.Type}] for resource [{@event.Value.Kind}/{@event.Value.Name()}]");

                            using (var timer = metrics.WorkDurationSeconds.NewTimer())
                            {
                                await eventHandler?.Invoke(@event);
                            }
                        }
                        finally
                        {
                            currentEvents.Remove(uid, out _);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
                throw;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                logger?.LogInformationEx(() => "Finalizer consumer cancelled.");
            }
        }

        /// <summary>
        /// Used to notify the queue of a new reconcilliation request. This will make sure that any pending
        /// requeue requests are cancelled, since they are no longer valid.
        /// </summary>
        /// <param name="event">The watch event being queued.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public async Task NotifyAsync(WatchEvent<TEntity> @event, CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            Covenant.Requires<ArgumentNullException>(@event != null, nameof(@event));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var queuedEvent = queue.Keys.Where(key => key.Value.Uid() == @event.Value.Uid()).FirstOrDefault();

                if (queuedEvent != null)
                {
                    if (@event.Value.Generation() > queuedEvent.Value.Generation())
                    {
                        await DequeueAsync(queuedEvent, cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
                throw;
            }
        }

        /// <summary>
        /// Requeue an event.
        /// </summary>
        /// <param name="event">Specifies the watch event being queued.</param>
        /// <param name="watchEventType">Optionally specifies the event type.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public async Task EnqueueAsync(
            WatchEvent<TEntity> @event,
            WatchEventType?     watchEventType    = null,
            CancellationToken   cancellationToken = default)
        {
            await SyncContext.Clear;

            Covenant.Requires<ArgumentNullException>(@event != null, nameof(@event));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var resource = @event.Value;

                logger?.LogDebugEx(() => $"Queuing event [{@event.Type}] for resource [{resource.Kind}/{resource.Name()}] ");

                if (queue.Keys.Any(key => key.Value.Uid() == @event.Value.Uid()))
                {
                    logger?.LogInformationEx(() => $"Event [{@event.Type}] already exists for resource [{resource.Kind}/{resource.Name()}], aborting");
                    return;
                }

                if (watchEventType == null)
                {
                    watchEventType = @event.Type;
                }

                @event.Type = watchEventType.Value;

                var cts = new CancellationTokenSource();

                if (queue.TryAdd(@event, cts))
                {
                    metrics.AddsTotal.Inc();
                    metrics.Depth.Set(queue.Count);
                }

                switch (@event.ModifiedEventType)
                {
                    case Controllers.ModifiedEventType.Finalizing:

                        logger?.LogDebugEx(() => $"Writing [{@event.Type}] [{resource.Kind}/{resource.Name()}] to finalizer channel.");

                        await finalizeChannel.Writer.WriteAsync(@event.Value.Uid(), cancellationToken);

                        break;

                    default:

                        logger?.LogDebugEx(() => $"Writing [{@event.Type}] [{resource.Kind}/{resource.Name()}] to event channel.");

                        await eventChannel.Writer.WriteAsync(@event.Value.Uid(), cancellationToken);

                        break;
                }
            }
            catch (Exception e)
            {
                logger?.LogErrorEx(e);
                throw;
            }
        }

        /// <summary>
        /// Queue an event, but dequeue existing event first.
        /// </summary>
        /// <param name="event">Specifies the watch event being queued.</param>
        /// <param name="delay">
        /// Optionally specifies the time to delay before requeuing the event.  This
        /// defaults to a computed value based on the number of times reconcile has
        /// been attempted.
        /// </param>
        /// <param name="watchEventType">Optionally specifies the watch event type.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public async Task RequeueAsync(
            WatchEvent<TEntity> @event,
            TimeSpan?           delay             = null, 
            WatchEventType?     watchEventType    = null,
            CancellationToken   cancellationToken = default)
        {
            await SyncContext.Clear;

            Covenant.Requires<ArgumentNullException>(@event != null, nameof(@event));

            cancellationToken.ThrowIfCancellationRequested();

            logger?.LogDebugEx(() => $"Requeuing resource [{@event.Value.Kind}/{@event.Value.Name()}]. Attempt [{@event.Attempt}]");

            metrics.RetriesTotal.Inc();

            try
            {
                var resource       = @event.Value;
                var existingEvents = queue.Keys
                    .Where(key => key.Value.Uid() == @event.Value.Uid());

                foreach (var existingEvent in existingEvents)
                {
                    await DequeueAsync(existingEvent, cancellationToken);

                    if (existingEvent.CreatedAt > @event.CreatedAt)
                    {
                        @event.Value = existingEvent.Value;
                    }
                }

                if (delay == null && @event.Attempt > 0)
                {
                    delay = GetDelay(@event.Attempt);

                    logger?.LogDebugEx(() => $"Event [{@event.Type}] delay for resource [{resource.Kind}/{resource.Name()}]: {delay}");
                }

                if (delay > TimeSpan.Zero)
                {
                    _ = EnqueueAfterSleepAsync(@event, delay.Value, watchEventType, cancellationToken);

                    return;
                }
            }
            catch (Exception e)
            {
                logger?.LogDebugEx(e);
            }

            @event.CreatedAt = DateTime.UtcNow;

            await EnqueueAsync(@event, watchEventType, cancellationToken);
        }

        /// <summary>
        /// Enqueue an watch event after a specified delay.
        /// </summary>
        /// <param name="event">Specifies the watch event.</param>
        /// <param name="delay">Specifies the delay.</param>
        /// <param name="watchEventType">Optionally specifies the watch event type.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task EnqueueAfterSleepAsync(
            WatchEvent<TEntity> @event,
            TimeSpan            delay,
            WatchEventType?     watchEventType    = null,
            CancellationToken   cancellationToken = default)
        {
            await SyncContext.Clear;

            Covenant.Requires<ArgumentNullException>(@event != null, nameof(@event));
            Covenant.Requires<ArgumentNullException>(delay >= TimeSpan.Zero, nameof(delay));

            cancellationToken.ThrowIfCancellationRequested();

            logger?.LogDebugEx(() => $"Sleeping before executing event [{@event.Type}] for resource [{@event.Value.Kind}/{@event.Value.Name()}]");

            await Task.Delay(delay);

            @event.CreatedAt = DateTime.UtcNow;

            await EnqueueAsync(@event, watchEventType, cancellationToken);
        }

        /// <summary>
        /// Dequeues an event.
        /// </summary>
        /// <param name="event">Specifies the watch event being dequeued.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public async Task DequeueAsync(WatchEvent<TEntity> @event, CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            Covenant.Requires<ArgumentNullException>(@event != null, nameof(@event));

            cancellationToken.ThrowIfCancellationRequested();

            var queuedEvent = queue.Keys.Where(key => key.Value.Uid() == @event.Value.Uid()).FirstOrDefault();

            if (queuedEvent == null) 
            { 
                return; 
            }

            if (queuedEvent.Value != null)
            {
                logger?.LogDebugEx(() => $"Dequeuing resource [{@event.Value.Kind}/{@event.Value.Name()}].");

                if (!queue[queuedEvent].IsCancellationRequested)
                {
                    queue[queuedEvent].Cancel();
                }

                if (queue.TryRemove(queuedEvent, out _))
                {
                    metrics.Depth.Set(queue.Count);
                }
            }
        }

        /// <summary>
        /// Computes the default requeuing delay based on the number of reconcile attempts so far
        /// </summary>
        /// <param name="attempts">The current number of reconcile attempts.</param>
        /// <returns>The delat <see cref="TimeSpan"/>.</returns>
        private TimeSpan GetDelay(int attempts)
        {
            Covenant.Requires<ArgumentException>(attempts >= 0, nameof(attempts));

            return TimeSpan.FromMilliseconds(Math.Min(options.ErrorMinRequeueInterval.TotalMilliseconds * (attempts), options.ErrorMaxRequeueInterval.TotalMilliseconds));
        }
    }
}

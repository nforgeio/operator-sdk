﻿//-----------------------------------------------------------------------------
// FILE:	    EventQueueMetrics.cs
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
using System.Diagnostics.Contracts;

using k8s;
using k8s.Models;

using Prometheus;

namespace Neon.Operator.EventQueue
{
    /// <summary>
    /// Used for maintaining event queue metrics.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the entity type.</typeparam>
    /// <typeparam name="TController">Specifies the controller type.</typeparam>
    internal class EventQueueMetrics<TEntity, TController>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private const string prefix = "operator_eventqueue";
        
        private static readonly string[] LabelNames = { "operator", "controller", "kind", "group", "version" };

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="operatorSettings">Specifies the operator settings.</param>
        public EventQueueMetrics(OperatorSettings operatorSettings) 
        {
            Covenant.Requires<ArgumentNullException>(operatorSettings != null, nameof(operatorSettings));

            var crdMeta     = typeof(TEntity).GetKubernetesTypeMetadata();
            var labelValues = new string[] 
            { 
                operatorSettings.Name, 
                typeof(TController).Name.ToLower(), 
                crdMeta.PluralName, 
                crdMeta.Group, 
                crdMeta.ApiVersion 
            };

            AddsTotal = Metrics
                .CreateCounter(
                    name: $"{prefix}_adds_total",
                    help: "The total number of queued items.",
                    labelNames: LabelNames,
                    configuration: new CounterConfiguration() { ExemplarBehavior = operatorSettings.ExemplarBehavior })
                .WithLabels(labelValues);

            RetriesTotal = Metrics
                .CreateCounter(
                    name: $"{prefix}_retries_total",
                    help: "The total number of retries.",
                    labelNames: LabelNames,
                    configuration: new CounterConfiguration() { ExemplarBehavior = operatorSettings.ExemplarBehavior })
                .WithLabels(labelValues);

            Depth = Metrics
                .CreateGauge(
                    name: $"{prefix}_depth", 
                    help: "The current depth of the event queue.",
                    labelNames: LabelNames)
                .WithLabels(labelValues);

            QueueDurationSeconds = Metrics
                .CreateHistogram(
                    name: $"{prefix}_queue_duration_seconds",
                    help: "How long in seconds an item stays in the event queue before being handled by the controller.",
                    labelNames: LabelNames,
                    configuration: new HistogramConfiguration() { ExemplarBehavior = operatorSettings.ExemplarBehavior })
                .WithLabels(labelValues);

            WorkDurationSeconds = Metrics
                .CreateHistogram(
                    name: $"{prefix}_work_duration_seconds",
                    help: "How long in seconds it takes to process an item from the queue.",
                    labelNames: LabelNames,
                    configuration: new HistogramConfiguration() { ExemplarBehavior = operatorSettings.ExemplarBehavior })
                .WithLabels(labelValues);

            UnfinishedWorkSeconds = Metrics
                .CreateGauge(
                    name: $"{prefix}_unfinished_work_seconds",
                    help: "The amount of seconds that work is in progress without completing. Large values indicate stuck threads.",
                    labelNames: LabelNames)
                .WithLabels(labelValues);

            LongestRunningProcessorSeconds = Metrics
                .CreateGauge(
                    name: $"{prefix}_longest_running_processor_seconds",
                    help: "The amount of seconds that work is in progress without completing. Large values indicate stuck threads.",
                    labelNames: LabelNames)
                .WithLabels(labelValues);

            ActiveWorkers = Metrics
                .CreateGauge(
                    name: $"{prefix}_active_workers",
                    help: "The number of currently active reconcilers.",
                    labelNames: LabelNames)
                .WithLabels(labelValues);

            MaxActiveWorkers = Metrics
                .CreateGauge(
                    name: $"{prefix}_max_active_workers",
                    help: "Total number of reconciliations per controller.",
                    labelNames: LabelNames)
                .WithLabels(labelValues);
        }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Counter.Child AddsTotal { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Counter.Child RetriesTotal { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Gauge.Child Depth { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Histogram.Child QueueDurationSeconds { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Histogram.Child WorkDurationSeconds { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Gauge.Child UnfinishedWorkSeconds { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Gauge.Child LongestRunningProcessorSeconds { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Gauge.Child ActiveWorkers { get; private set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Gauge.Child MaxActiveWorkers { get; private set; }
    }
}

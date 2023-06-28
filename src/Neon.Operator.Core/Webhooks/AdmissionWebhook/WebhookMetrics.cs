//-----------------------------------------------------------------------------
// FILE:	    WebhookMetrics.cs
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

using Prometheus;

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// Webhook related metrics.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class WebhookMetrics<TEntity>
    {
        private const string prefix = "operator_webhook";

        private static readonly string[]    LabelNames = { "operator", "webhook" };
        private readonly string             operatorName;
        private readonly string             webhook;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="operatorSettings"></param>
        /// <param name="webhook"></param>
        public WebhookMetrics(OperatorSettings operatorSettings, string webhook) 
        {
            this.operatorName = operatorSettings.Name;
            this.webhook      = webhook;

            var labelValues = new string[] { operatorName, webhook };

            LatencySeconds = Metrics
                .CreateHistogram(
                    name: $"{prefix}_latency_seconds",
                    help: "Histogram of the latency of processing admission requests.", 
                    labelNames: LabelNames,
                    configuration: new HistogramConfiguration() { ExemplarBehavior = operatorSettings.ExemplarBehavior })
                .WithLabels(labelValues);

            RequestsTotal = Metrics
                .CreateCounter(
                    name: $"{prefix}_requests_total",
                    help: "Total number of admission requests by HTTP status code.",
                    labelNames: new string[] { "operator", "webhook", "code" },
                    configuration: new CounterConfiguration() { ExemplarBehavior = operatorSettings.ExemplarBehavior });

            RequestsInFlight = Metrics
                .CreateGauge(
                    name: $"{prefix}_requests_in_flight",
                    help: "Current number of admission requests being served.",
                    labelNames: LabelNames)
                .WithLabels(labelValues);
        }
        
        /// <summary>
        /// Latency in seconds.
        /// </summary>
        public Histogram.Child LatencySeconds { get; private set; }

        /// <summary>
        /// Request count.
        /// </summary>
        public Counter RequestsTotal { get; private set; }

        /// <summary>
        /// Requests currently in flight.
        /// </summary>
        public Gauge.Child RequestsInFlight { get; private set; }
    }
}

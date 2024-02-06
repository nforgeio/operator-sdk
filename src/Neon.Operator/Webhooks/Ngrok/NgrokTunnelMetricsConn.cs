//-----------------------------------------------------------------------------
// FILE:	    NgrokTunnelMetricsConn.cs
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

using System.Text.Json.Serialization;

namespace Neon.Operator.Webhooks.Ngrok
{
    /// <summary>
    /// Ngrok tunnel metrics.
    /// </summary>
    public class NgrokTunnelMetricsConn
    {
        /// <summary>
        /// The tunnel count.
        /// </summary>
        [JsonPropertyName("count")]
        public long? Count { get; set; }

        /// <summary>
        /// Gauge.
        /// </summary>
        [JsonPropertyName("gauge")]
        public long? Gauge { get; set; }

        /// <summary>
        /// Rate 1.
        /// </summary>
        [JsonPropertyName("rate1")]
        public long? Rate1 { get; set; }

        /// <summary>
        /// Rate 5.
        /// </summary>
        [JsonPropertyName("rate5")]
        public long? Rate5 { get; set; }

        /// <summary>
        /// Rate 15.
        /// </summary>
        [JsonPropertyName("rate15")]
        public long? Rate15 { get; set; }

        /// <summary>
        /// P50.
        /// </summary>
        [JsonPropertyName("p50")]
        public long? P50 { get; set; }

        /// <summary>
        /// P90.
        /// </summary>
        [JsonPropertyName("p90")]
        public long? P90 { get; set; }

        /// <summary>
        /// P95.
        /// </summary>
        [JsonPropertyName("p95")]
        public long? P95 { get; set; }

        /// <summary>
        /// P99.
        /// </summary>
        [JsonPropertyName("p99")]
        public long? P99 { get; set; }
     }
}

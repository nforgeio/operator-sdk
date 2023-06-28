//-----------------------------------------------------------------------------
// FILE:	    NgrokTunnelDetail.cs
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

using Newtonsoft.Json;

namespace Neon.Operator.Webhooks.Ngrok
{
    /// <summary>
    /// Ngrok tunnel details.
    /// </summary>
    public class NgrokTunnelDetail
    {
        /// <summary>
        /// Tunnel name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Tunnel URL.
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// The tunnel public URL.
        /// </summary>
        [JsonProperty("public_url")]
        public string PublicUrl { get; set; }

        /// <summary>
        /// The tunnel protocol.
        /// </summary>
        [JsonProperty("proto")]
        public string Proto { get; set; }

        /// <summary>
        /// The <see cref="NgrokTunnelConfig"/>.
        /// </summary>
        [JsonProperty("config")]
        public NgrokTunnelConfig Config { get; set; }

        /// <summary>
        /// The <see cref="NgrokTunnelMetrics"/>.
        /// </summary>
        [JsonProperty("metrics")]
        public NgrokTunnelMetrics Metrics { get; set; }
    }
}

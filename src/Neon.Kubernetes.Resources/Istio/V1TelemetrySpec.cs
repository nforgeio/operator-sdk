//-----------------------------------------------------------------------------
// FILE:        V1TelemetrySpec.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

using System.Collections.Generic;
using System.ComponentModel;

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// Describes a Telemetry spec.
    /// </summary>
    public class V1TelemetrySpec
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public V1TelemetrySpec()
        {
        }

        /// <summary>
        /// The tracing config.
        /// </summary>
        [DefaultValue(null)]
        public List<Tracing> Tracing { get; set; }
    }
}

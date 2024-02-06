//-----------------------------------------------------------------------------
// FILE:        V1GrafanaDashboardSpec.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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

namespace Neon.K8s.Resources.Grafana
{
    /// <summary>
    /// Grafana Dashboard.
    /// </summary>
    public class V1GrafanaDashboardSpec
    {
        /// <summary>
        /// The list of data sources.
        /// </summary>
        [DefaultValue(null)]
        public List<V1GrafanaDatasource> Datasources { get; set; }

        /// <summary>
        /// The JSON describing the dashboard.
        /// </summary>
        [DefaultValue(null)]
        public string Json { get; set; }
    }
}

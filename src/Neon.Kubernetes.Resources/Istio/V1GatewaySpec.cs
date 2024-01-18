//-----------------------------------------------------------------------------
// FILE:        V1GatewaySpec.cs
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
using System.ComponentModel.DataAnnotations;

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// Describes a load balancer operating at the edge of the mesh receiving incoming or outgoing HTTP/TCP connections.
    /// </summary>
    public class V1GatewaySpec
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public V1GatewaySpec()
        {
        }

        /// <summary>
        /// One or more labels that indicate a specific set of pods/VMs on which this gateway configuration should be applied. By default 
        /// workloads are searched across all namespaces based on label selectors. This implies that a gateway resource in the namespace 
        /// “foo” can select pods in the namespace “bar” based on labels. This behavior can be controlled via the PILOT_SCOPE_GATEWAY_TO_NAMESPACE 
        /// environment variable in istiod. If this variable is set to true, the scope of label search is restricted to the configuration namespace 
        /// in which the the resource is present. In other words, the Gateway resource must reside in the same namespace as the gateway workload 
        /// instance. If selector is nil, the Gateway will be applied to all workloads.
        /// </summary>
        [Required]
        [DefaultValue(null)]
        public Dictionary<string, string> Selector { get; set; }

        /// <summary>
        /// Describes the properties of the proxy on a given load balancer port.
        /// </summary>
        [Required]
        [DefaultValue(null)]
        public List<Server> Servers { get; set; }
    }
}

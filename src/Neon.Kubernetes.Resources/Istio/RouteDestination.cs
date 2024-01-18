//-----------------------------------------------------------------------------
// FILE:        RouteDestination.cs
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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using k8s;

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// L4 routing rule weighted destination.
    /// </summary>
    public class RouteDestination : IValidate
    {
        /// <summary>
        /// Initializes a new instance of the RouteDestination class.
        /// </summary>
        public RouteDestination()
        {
        }

        /// <summary>
        /// Destination uniquely identifies the instances of a service to which the request/connection should be forwarded to.
        /// </summary>
        [Required]
        [DefaultValue(null)]
        public Destination Destination { get; set; }

        /// <summary>
        /// <para>
        /// The proportion of traffic to be forwarded to the service version. If there is only one destination in a rule, all traffic 
        /// will be routed to it irrespective of the weight.
        /// </para>
        /// </summary>
        [DefaultValue(null)]
        public int? weight { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

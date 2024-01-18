//-----------------------------------------------------------------------------
// FILE:        TCPRoute.cs
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

using k8s;

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// Describes match conditions and actions for routing TCP traffic. The following routing rule forwards traffic arriving at port 
    /// 27017 for mongo.prod.svc.cluster.local to another Mongo server on port 5555.
    /// </summary>
    public class TCPRoute : IValidate
    {
        /// <summary>
        /// Initializes a new instance of the TCPRoute class.
        /// </summary>
        public TCPRoute()
        {
        }

        /// <summary>
        /// <para>
        /// Match conditions to be satisfied for the rule to be activated. All conditions inside a single match block have AND semantics, 
        /// while the list of match blocks have OR semantics. The rule is matched if any one of the match blocks succeed.
        /// </para>
        /// </summary>
        [DefaultValue(null)]
        public List<L4MatchAttributes> Match { get; set; }

        /// <summary>
        /// The destination to which the connection should be forwarded to.
        /// </summary>
        public List<RouteDestination> Route { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

//-----------------------------------------------------------------------------
// FILE:        Headers.cs
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
    /// Message headers can be manipulated when Envoy forwards requests to, or responses from, a destination service. Header manipulation 
    /// rules can be specified for a specific route destination or for all destinations. The following V1VirtualService adds a test header with 
    /// the value true to requests that are routed to any reviews service destination. It also removes the foo response header, but only from
    /// responses coming from the v1 subset (version) of the reviews service.
    /// </summary>
    public class Headers : IValidate
    {
        /// <summary>
        /// Initializes a new instance of the Headers class.
        /// </summary>
        public Headers()
        {
        }

        /// <summary>
        /// Header manipulation rules to apply before forwarding a request to the destination service
        /// </summary>
        [DefaultValue(null)]
        public HeaderOperations Request { get; set; }

        /// <summary>
        /// Header manipulation rules to apply before returning a response to the caller
        /// </summary>
        [DefaultValue(null)]
        public HeaderOperations Response { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

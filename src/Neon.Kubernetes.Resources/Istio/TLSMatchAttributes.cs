//-----------------------------------------------------------------------------
// FILE:        TLSMatchAttributes.cs
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
    /// TLS connection match attributes.
    /// </summary>
    public class TLSMatchAttributes : IValidate
    {
        /// <summary>
        /// Initializes a new instance of the TLSMatchAttributes class.
        /// </summary>
        public TLSMatchAttributes()
        {
        }

        /// <summary>
        /// <para>
        /// SNI (server name indicator) to match on. Wildcard prefixes can be used in the SNI value, e.g., *.com will match foo.example.com as 
        /// well as example.com. An SNI value must be a subset (i.e., fall within the domain) of the corresponding virtual serivce’s hosts.
        /// </para>
        /// </summary>
        [Required]
        [DefaultValue(null)]
        public List<string> SniHosts { get; set; }

        /// <summary>
        /// <para>
        /// IPv4 or IPv6 ip addresses of destination with optional subnet. E.g., a.b.c.d/xx form or just a.b.c.d.
        /// </para>
        /// </summary>
        [DefaultValue(null)]
        public List<string> DestinationSubnets { get; set; }

        /// <summary>
        /// <para>
        /// Specifies the port on the host that is being addressed. Many services only expose a single port or label ports with the protocols they 
        /// support, in these cases it is not required to explicitly select the port.
        /// </para>
        /// </summary>
        [DefaultValue(null)]
        public int? Port { get; set; }

        /// <summary>
        /// <para>
        /// One or more labels that constrain the applicability of a rule to workloads with the given labels. If the V1VirtualService has a list of gateways 
        /// specified in the top-level gateways field, it should include the reserved gateway mesh in order for this field to be applicable.
        /// </para>
        /// </summary>
        [DefaultValue(null)]
        public Dictionary<string, string> SourceLabels { get; set; }

        /// <summary>
        /// <para>
        /// Names of gateways where the rule should be applied. Gateway names in the top-level gateways field of the V1VirtualService (if any) 
        /// are overridden. The gateway match is independent of sourceLabels.
        /// </para>
        /// </summary>
        [DefaultValue(null)]
        public List<string> Gateways { get; set; }

        /// <summary>
        /// <para>
        /// Source namespace constraining the applicability of a rule to workloads in that namespace. If the V1VirtualService has a list of gateways 
        /// specified in the top-level gateways field, it must include the reserved gateway mesh for this field to be applicable.
        /// </para>
        /// </summary>
        [DefaultValue(null)]
        public string SourceNamespace { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

//-----------------------------------------------------------------------------
// FILE:	    CertManagerOptions.cs
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

using System;
using System.ComponentModel;

using Newtonsoft.Json;

namespace Neon.Operator
{
    /// <summary>
    /// Certificate manager related options.
    /// </summary>
    public class CertManagerOptions
    {
        /// <summary>
        /// Specifies the certificate lifespan.
        /// </summary>
        public TimeSpan CertificateDuration { get; set; }

        /// <summary>
        /// The Issuer that should issue the certificate.
        /// </summary>
        public IssuerRef IssuerRef { get; set; }
    }

    /// <summary>
    /// CertManager Issuer Ref.
    /// </summary>
    public class IssuerRef
    {
        /// <summary>
        /// The Group.
        /// </summary>
        [JsonProperty(PropertyName = "group", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Group { get; set; }

        /// <summary>
        /// The kind.
        /// </summary>
        [JsonProperty(PropertyName = "kind", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Kind { get; set; }

        /// <summary>
        /// The name.
        /// </summary>
        [JsonProperty(PropertyName = "name", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Name { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public IssuerRef()
        {
        }
    }
}

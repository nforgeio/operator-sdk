//-----------------------------------------------------------------------------
// FILE:        V1IssuerSpec.cs
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

using System.ComponentModel;

namespace Neon.K8s.Resources.CertManager
{
    /// <summary>
    /// The kubernetes spec for a cert-manager Issuer.
    /// </summary>
    public class V1IssuerSpec
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public V1IssuerSpec()
        {
        }

        /// <summary>
        /// ACME configures this issuer to communicate with a RFC8555 (ACME) server to obtain signed x509 certificates.
        /// </summary>
        [DefaultValue(null)]
        public AcmeIssuer Acme { get; set; } = null;

        /// <inheritdoc/>
        public void Validate()
        {
            var issuerSpecPrefix = $"{nameof(V1IssuerSpec)}";

            Acme = Acme ?? new AcmeIssuer();
            Acme.Validate();
        }
    }
}

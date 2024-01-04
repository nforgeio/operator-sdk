//-----------------------------------------------------------------------------
// FILE:        PKCS12.cs
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

using Newtonsoft.Json;

namespace Neon.K8s.Resources.CertManager
{
    /// <summary>
    /// Configures options for storing a PKCS12 keystore in the `spec.secretName` Secret resource.
    /// </summary>
    public class PKCS12
    {
        /// <summary>
        /// Initializes a new instance of the PKCS12 class.
        /// </summary>
        public PKCS12()
        {
        }

        /// <summary>
        /// Configures options for storing a PKCS12 keystore in the `spec.secretName` Secret resource.
        /// </summary>
        [JsonProperty(PropertyName = "create", Required = Required.Always, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool Create { get; set; }

        /// <summary>
        /// A reference to a key in a Secret resource containing the password used to encrypt the PKCS12 keystore.
        /// </summary>
        [JsonProperty(PropertyName = "passwordSecretRef", Required = Required.Always, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public PasswordSecretRef PasswordSecretRef { get; set; }
    }
}

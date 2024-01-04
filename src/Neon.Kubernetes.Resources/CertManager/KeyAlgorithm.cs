//-----------------------------------------------------------------------------
// FILE:        KeyAlgorithm.cs
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

using System.Runtime.Serialization;

using Newtonsoft.Json.Converters;

namespace Neon.K8s.Resources.CertManager
{
    /// <summary>
    /// The private key algorithm of the corresponding private key for this certificate. If provided, 
    /// allowed values are either `RSA` or `ECDSA` If `algorithm` is specified and `size` is not provided,
    /// key size of 256 will be used for `ECDSA` key algorithm and key size of 2048 will be used for 
    /// `RSA` key algorithm.
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumMemberConverter))]
    public enum KeyAlgorithm
    {
        /// <summary>
        /// RSA#1
        /// </summary>
        [EnumMember(Value = "RSA")]
        RSA = 0,

        /// <summary>
        /// ECDSA#8
        /// </summary>
        [EnumMember(Value = "ECDSA")]
        ECDSA,

        /// <summary>
        /// ECDSA#8
        /// </summary>
        [EnumMember(Value = "Ed25519")]
        Ed25519
    }
}

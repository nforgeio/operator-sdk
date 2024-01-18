//-----------------------------------------------------------------------------
// FILE:        PortProtocol.cs
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

using System.Runtime.Serialization;

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// The protocol exposed on the port.
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumMemberConverter))]
    public enum PortProtocol
    {
        /// <summary>
        /// HTTP
        /// </summary>
        [EnumMember(Value = "HTTP")]
        HTTP = 0,

        /// <summary>
        /// HTTPS
        /// </summary>
        [EnumMember(Value = "HTTPS")]
        HTTPS,

        /// <summary>
        /// GRPC
        /// </summary>
        [EnumMember(Value = "GRPC")]
        GRPC,

        /// <summary>
        /// HTTP2
        /// </summary>
        [EnumMember(Value = "HTTP2")]
        HTTP2,

        /// <summary>
        /// MONGO
        /// </summary>
        [EnumMember(Value = "MONGO")]
        MONGO,

        /// <summary>
        /// TCP
        /// </summary>
        [EnumMember(Value = "TCP")]
        TCP,

        /// <summary>
        /// TLS
        /// </summary>
        [EnumMember(Value = "TLS")]
        TLS
    }
}

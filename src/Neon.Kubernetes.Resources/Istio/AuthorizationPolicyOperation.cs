//-----------------------------------------------------------------------------
// FILE:        AuthorizationPolicyOperation.cs
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

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// Specifies the operations of a request. Fields in the operation are ANDed together.
    /// </summary>
    public class AuthorizationPolicyOperation
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AuthorizationPolicyOperation()
        {

        }

        /// <summary>
        /// <para>
        /// A list of hosts as specified in the HTTP request. The match is case-insensitive. See the security 
        /// best practices for recommended usage of this field.
        /// </para>
        /// <remarks>
        /// If not set, any host is allowed. Must be used only with HTTP.
        /// </remarks>
        /// </summary>
        public List<string> Hosts { get; set; } = null;

        /// <summary>
        /// A list of negative match of peer <see cref="Hosts"/>s as specified in the HTTP request. 
        /// The match is case-insensitive.
        /// </summary>
        public List<string> NotHosts { get; set; } = null;

        /// <summary>
        /// <para>
        /// A list of ports as specified in the connection.
        /// </para>
        /// <remarks>
        /// If not set, any port is allowed.
        /// </remarks>
        /// </summary>
        public List<string> Ports { get; set; } = null;

        /// <summary>
        /// A list of negative match of request <see cref="Ports"/>.
        /// </summary>
        public List<string> NotPorts { get; set; } = null;

        /// <summary>
        /// <para>
        /// A list of methods as specified in the HTTP request. For gRPC service, 
        /// this will always be “POST”.
        /// </para>
        /// <remarks>
        /// If not set, any method is allowed. Must be used only with HTTP.
        /// </remarks>
        /// </summary>
        public List<string> Methods { get; set; } = null;

        /// <summary>
        /// A list of negative match of <see cref="Methods"/>.
        /// </summary>
        public List<string> NotMethods { get; set; } = null;

        /// <summary>
        /// <para>
        /// A list of paths as specified in the HTTP request. See the Authorization Policy Normalization 
        /// for details of the path normalization. For gRPC service, this will be the fully-qualified name 
        /// in the form of “/package.service/method”.
        /// </para>
        /// <remarks>
        /// If not set, any path is allowed.
        /// </remarks>
        /// </summary>
        public List<string> Paths { get; set; } = null;

        /// <summary>
        /// A list of negative match of <see cref="Paths"/>.
        /// </summary>
        public List<string> NotPaths { get; set; } = null;
    }
}

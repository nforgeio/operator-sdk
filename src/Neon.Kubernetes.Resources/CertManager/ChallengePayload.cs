//-----------------------------------------------------------------------------
// FILE:        ChallengePayload.cs
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

namespace Neon.K8s.Resources.CertManager
{
    /// <summary>
    /// Describes a request/response for presenting or cleaning up
    /// an ACME challenge resource
    /// </summary>
    public class ChallengePayload
    {
        /// <summary>
        /// Gets or sets APIVersion defines the versioned schema of this
        /// representation of an object. Servers should convert recognized
        /// schemas to the latest internal value, and may reject unrecognized
        /// values. More info:
        /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#resources
        /// </summary>
        [DefaultValue(null)]
        public string ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets kind is a string value representing the REST resource
        /// this object represents. Servers may infer this from the endpoint
        /// the client submits requests to. Cannot be updated. In CamelCase.
        /// More info:
        /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#types-kinds
        /// </summary>
        [DefaultValue(null)]
        public string Kind { get; set; }

        /// <summary>
        /// Describes the attributes for the ACME solver request.
        /// </summary>
        [DefaultValue(null)]
        public ChallengeRequest Request { get; set; }

        /// <summary>
        /// Describes the attributes for the ACME solver response.
        /// </summary>
        [DefaultValue(null)]
        public ChallengeResponse Response { get; set; }
    }
}

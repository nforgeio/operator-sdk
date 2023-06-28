//-----------------------------------------------------------------------------
// FILE:	    AdmissionResponse.cs
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

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// $todo(marcusbooyah): Documentation
    /// </summary>
    public sealed class AdmissionResponse
    {
        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public const string JsonPatch = "JSONPatch";

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public bool Allowed { get; set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public Reason Status { get; set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public string PatchType { get; set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public string Patch { get; set; }

        /// <summary>
        /// $todo(marcusbooyah): Documentation
        /// </summary>
        public sealed class Reason
        {
            /// <summary>
            /// $todo(marcusbooyah): Documentation
            /// </summary>
            public int Code { get; set; }

            /// <summary>
            /// $todo(marcusbooyah): Documentation
            /// </summary>
            public string Message { get; set; } = string.Empty;
        }
    }
}
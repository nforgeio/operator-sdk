// -----------------------------------------------------------------------------
// FILE:	    FailurePolicy.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// <para>
    /// Defines how unrecognized errors and timeout errors from the admission webhook are handled.
    /// Allowed values are Ignore or Fail.
    /// </para>
    /// </summary>
    public enum FailurePolicy
    {
        /// <summary>
        /// Ignore means that an error calling the webhook is ignored and the API request is allowed to continue.
        /// </summary>
        [EnumMember(Value = "Ignore")]
        Ignore = 0,

        /// <summary>
        /// Fail means that an error calling the webhook causes the admission to fail and the API request to be rejected.
        /// </summary>
        [EnumMember(Value = "Fail")]
        Fail
    }
}

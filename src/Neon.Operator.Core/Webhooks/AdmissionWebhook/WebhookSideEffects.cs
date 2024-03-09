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

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// Defines how unrecognized errors and timeout errors from the admission webhook are handled.
    /// Allowed values are Ignore or Fail.
    /// </summary>
    public enum SideEffects
    {
        /// <summary>
        /// Calling the webhook will have no side effects.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// Calling the webhook will possibly have side effects, but if a request with dryRun: true
        /// is sent to the webhook, the webhook will suppress the side effects (the webhook is dryRun-aware).
        /// </summary>
        [EnumMember(Value = "NoneOnDryRun")]
        NoneOnDryRun
    }
}

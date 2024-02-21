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
    /// <para>
    /// API servers can make objects available via multiple API groups or versions.
    /// For example, if a webhook only specified a rule for some API groups/versions(like apiGroups:["apps"],
    /// apiVersions:["v1","v1beta1"]), and a request was made to modify the resource via another API
    /// group/version(like extensions/v1beta1), the request would not be sent to the webhook.
    /// </para>
    ///
    /// <para>
    /// The matchPolicy lets a webhook define how its rules are used to match incoming requests.
    /// Allowed values are Exact or Equivalent.
    /// </para>
    /// </summary>
    public enum MatchPolicy
    {
        /// <summary>
        /// Exact would mean the extensions/v1beta1 request would not be sent to the webhook
        /// </summary>
        [EnumMember(Value = "Exact")]
        Exact = 0,

        /// <summary>
        /// Equivalent means the extensions/v1beta1 request would be sent to the webhook
        /// (with the objects converted to a version the webhook had specified: apps/v1)
        /// </summary>
        [EnumMember(Value = "Equivalent")]
        Equivalent
    }
}

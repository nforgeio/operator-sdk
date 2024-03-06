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
    /// A single ordering of mutating admissions plugins (including webhooks) does not work for all
    /// cases (see https://issue.k8s.io/64333 as an example). A mutating webhook can add a new sub-structure
    /// to the object (like adding a container to a pod), and other mutating plugins which have already run
    /// may have opinions on those new structures (like setting an imagePullPolicy on all containers).
    /// </para>
    /// <para>
    /// To allow mutating admission plugins to observe changes made by other plugins, built-in mutating
    /// admission plugins are re-run if a mutating webhook modifies an object, and mutating webhooks can
    /// specify a reinvocationPolicy to control whether they are reinvoked as well.
    /// </para>
    /// </summary>
    public enum ReinvocationPolicy
    {
        /// <summary>
        /// The webhook must not be called more than once in a single admission evaluation.
        /// </summary>
        [EnumMember(Value = "Never")]
        Never = 0,

        /// <summary>
        /// The webhook may be called again as part of the admission evaluation if the
        /// object being admitted is modified by other admission plugins after the initial webhook call.
        /// </summary>
        [EnumMember(Value = "IfNeeded")]
        IfNeeded
    }
}

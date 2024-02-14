// -----------------------------------------------------------------------------
// FILE:	    RequirementStatus.cs
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
using System.Reflection;
using System.Text;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// The status of each requirement for this CSV
    /// </summary>
    public class RequirementStatus
    {
        /// <summary>
        /// DependentStatus is the status for a dependent requirement (to prevent infinite nesting)
        /// </summary>
        public List<DependentStatus> Dependents {  get; set; }

        /// <summary>
        /// group is the group of the requirement
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Kind is the kind of the requirement
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Name is the name of the requirement
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// StatusReason is a camelcased reason for the status of a RequirementStatus or DependentStatus
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Uuid is the unique identifier for the requirement
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// Version is the version of the requirement
        /// </summary>
        public string Version { get; set; }


    }
}

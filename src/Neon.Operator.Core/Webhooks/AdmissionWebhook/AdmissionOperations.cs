//-----------------------------------------------------------------------------
// FILE:	    AdmissionOperations.cs
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

using System;
using System.Runtime.Serialization;

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// Represents admission controller operations.
    /// </summary>
    [Flags]
    public enum AdmissionOperations
    {
        /// <summary>
        /// None.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// All.
        /// </summary>
        [EnumMember(Value = "All")]
        All = 1,

        /// <summary>
        /// Create.
        /// </summary>
        [EnumMember(Value = "Create")]
        Create = 2,

        /// <summary>
        /// Update.
        /// </summary>
        [EnumMember(Value = "Update")]
        Update = 4,

        /// <summary>
        /// Delete.
        /// </summary>
        [EnumMember(Value = "Delete")]
        Delete = 8,
    }
}

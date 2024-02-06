//-----------------------------------------------------------------------------
// FILE:	    RbacVerb.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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

namespace Neon.Operator.Rbac
{
    /// <summary>
    /// Enumerates the Kubernetes RBAC verbs.
    /// </summary>
    [Flags]
    public enum RbacVerb
    {
        /// <summary>
        /// No permissions will be allowed.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// All permissions will be allowed.
        /// </summary>
        [EnumMember(Value = "All")]
        All = 1,

        /// <summary>
        /// Allows GET on the resource.
        /// </summary>
        [EnumMember(Value = "Get")]
        Get = 2,

        /// <summary>
        /// Allows listing all resources for the type.
        /// </summary>
        [EnumMember(Value = "List")]
        List = 8,

        /// <summary>
        /// Allows watching resources of the type.
        /// </summary>
        [EnumMember(Value = "Watch")]
        Watch =  16,

        /// <summary>
        /// Allows creating resources for the type.
        /// </summary>
        [EnumMember(Value = "Create")]
        Create = 32,

        /// <summary>
        /// Allows updating existing resources for the type.
        /// </summary>
        [EnumMember(Value = "Update")]
        Update = 64,

        /// <summary>
        /// Allows patching resources for the type.
        /// </summary>
        [EnumMember(Value = "Patch")]
        Patch = 128,

        /// <summary>
        /// Allows deleting resources for the type.
        /// </summary>
        [EnumMember(Value = "Delete")]
        Delete = 256,
    }
}

//-----------------------------------------------------------------------------
// FILE:	    RbacVerb.cs
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
using System.Runtime.Serialization;

namespace Neon.Operator.Rbac
{
    /// <summary>
    /// Enumerates the Kubernetes Subresources.
    /// </summary>
    [Flags]
    public enum Subresource
    {
        /// <summary>
        /// No permissions will be allowed.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// Status permissions will be allowed.
        /// </summary>
        [EnumMember(Value = "status")]
        Status = 1,

        /// <summary>
        /// Scale premissions will be allowed.
        /// </summary>
        [EnumMember(Value = "scale")]
        Scale = 2
    }
}

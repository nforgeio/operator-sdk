// -----------------------------------------------------------------------------
// FILE:	    InstallModeType.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// InstallModeType is a supported type of install mode for CSV installation
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumMemberConverter))]
    public enum InstallModeType
    {
        /// <summary>
        /// If supported, the operator can be a member
        /// of an OperatorGroup that selects its own namespace
        /// </summary>
        [EnumMember(Value = "OwnNamespace")]
        OwnNamespace,

        /// <summary>
        /// If supported, the operator can be a member
        /// of an OperatorGroup that selects one namespace
        /// </summary>
        [EnumMember(Value = "SingleNamespace")]
        SingleNamespace,

        /// <summary>
        /// If supported, the operator can be a member
        /// of an OperatorGroup that selects more than one namespace
        /// </summary>
        [EnumMember(Value = "MultiNamespace")]
        MultiNamespace,

        /// <summary>
        /// If supported, the operator can be a member of an OperatorGroup
        /// that selects all namespaces (target namespace set is the empty string “”)
        /// </summary>
        [EnumMember(Value = "AllNamespaces")]
        AllNamespaces,




    }
}

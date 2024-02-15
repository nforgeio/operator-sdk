// -----------------------------------------------------------------------------
// FILE:	    CapabilityLevel.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{
    public enum CapabilityLevel
    {
        /// <summary>
        /// basic install level
        /// </summary>
        [EnumMember(Value = "Basic Install")]
        BasicInstall = 0,

        /// <summary>
        /// Seamless Upgrade level
        /// </summary>
        [EnumMember(Value = "Seamless Upgrade")]
        SeamlessUpgrade = 1,

        /// <summary>
        /// full lifecycle level
        /// </summary>
        [EnumMember(Value = "Full Lifecycle")]
        FullLifecycle = 2,

        /// <summary>
        /// deep insights level
        /// </summary>
        [EnumMember(Value = "Deep Insights")]
        DeepInsights = 3,

        /// <summary>
        /// auto pilot level
        /// </summary>
        [EnumMember(Value = "Auto Pilot")]
        AutoPilot = 4







    }
}

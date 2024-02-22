// -----------------------------------------------------------------------------
// FILE:	    Type.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Type specify supported installation types
    /// </summary>
    public class InstallMode
    {
        /// <summary>
        ///  flag representing if the CSV supports it
        /// </summary>
        public bool Supported {  get; set; }

        /// <summary>
        /// InstallModeType is a supported type of install mode for CSV installation
        /// </summary>
        public InstallModeType Type { get; set; }
    }
}

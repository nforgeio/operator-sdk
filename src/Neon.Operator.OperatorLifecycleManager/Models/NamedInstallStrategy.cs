// -----------------------------------------------------------------------------
// FILE:	    NamedInstallStrategy.cs
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
    /// NamedInstallStrategy represents the block of an ClusterServiceVersion resource where the install strategy is specified.
    /// </summary>
    public class NamedInstallStrategy
    {
        /// <summary>
        /// strategy is the name of the strategy that will be used to install the operator.
        /// </summary>
        public string Strategy {  get; set; }

        /// <summary>
        /// StrategyDetailsDeployment represents the parsed details of a Deployment InstallStrategy.
        /// </summary>
        public StrategyDetailsDeployment Spec { get; set; }
    }
}

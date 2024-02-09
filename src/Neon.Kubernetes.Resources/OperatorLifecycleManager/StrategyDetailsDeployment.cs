// -----------------------------------------------------------------------------
// FILE:	    StrategyDetailsDeployment.cs
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// StrategyDetailsDeployment represents the parsed details of a Deployment InstallStrategy.
    /// </summary>
    public class StrategyDetailsDeployment
    {
        /// <summary>
        /// StrategyDeploymentPermission describe the rbac rules and service account needed by the install strategy
        /// </summary>
        public List<StrategyDeploymentPermission> ClusterPermissions {  get; set; }

        /// <summary>
        /// StrategyDeploymentSpec contains the name, spec and labels for the deployment ALM should create
        /// </summary>
        [Required]
        public List<StrategyDeploymentSpec> Deployments { get; set; }

        /// <summary>
        /// StrategyDeploymentPermission describe the rbac rules and service account needed by the install strategy
        /// </summary>
        public List<StrategyDeploymentPermission> Permissions {  get; set; }

    }
}

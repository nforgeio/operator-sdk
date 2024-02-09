// -----------------------------------------------------------------------------
// FILE:	    StrategyDeploymentSpec.cs
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

using k8s.Models;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// .StrategyDeploymentSpec contains the name, spec and labels for the deployment ALM should create
    /// </summary>
    public class StrategyDeploymentSpec
    {
        /// <summary>
        /// Set is a map of label:value. It implements Labels.
        /// </summary>
        public Dictionary<string, string> Label { get; set; }

        /// <summary>
        /// name of the deployment
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// DeploymentSpec is the specification of the desired behavior of the Deployment.
        /// </summary>
        [Required]
        public V1DeploymentSpec Spec {  get; set; }
    }
}

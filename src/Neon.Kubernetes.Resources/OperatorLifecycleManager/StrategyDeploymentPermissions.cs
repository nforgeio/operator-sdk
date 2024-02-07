// -----------------------------------------------------------------------------
// FILE:	    StrategyDeploymentPermissions.cs
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
using System.Text;

using k8s.Models;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    public class StrategyDeploymentPermissions
    {
        /// <summary>
        /// service account name needed by the install strategy
        /// </summary>
        public string ServiceAccountName { get; set; }

        /// <summary>
        /// PolicyRule holds information that describes a policy rule,
        /// but does not contain information about who the rule applies to or which namespace the rule applies to.
        /// </summary>
        public PolicyRule  Rules {get;set;}
    }
}

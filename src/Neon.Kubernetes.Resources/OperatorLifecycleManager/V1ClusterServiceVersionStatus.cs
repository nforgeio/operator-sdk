// -----------------------------------------------------------------------------
// FILE:	    V1ClusterServiceVersionStatus.cs
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

using k8s.Models;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// ClusterServiceVersionStatus represents information about the status of a CSV. Status may trail the actual state of a system.
    /// </summary>
    public class V1ClusterServiceVersionStatus
    {
        /// <summary>
        /// Last time the owned APIService certs were updated
        /// </summary>
        public string CertsLastUpdated { get; set; }

        /// <summary>
        /// Time the owned APIService certs will rotate next
        /// </summary>
        public string CertsRotateAt { get; set; }

        /// <summary>
        /// CleanupStatus represents information about the status of cleanup while a CSV is pending deletion
        /// </summary>
        public CleanupStatus Cleanup {  get; set; }

        /// <summary>
        /// Conditions appear in the status as a record of state transitions on the ClusterServiceVersion
        /// </summary>
        public List<Condition> Conditions { get; set; }

        /// <summary>
        /// Last time the status transitioned from one status to another.
        /// </summary>
        public string LastTransitionTime { get; set; }

        /// <summary>
        /// Last time we updated the status
        /// </summary>
        public string LastUpdateTime { get; set; }

        /// <summary>
        /// A human readable message indicating details about why the ClusterServiceVersion is in this condition.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Current condition of the ClusterServiceVersion
        /// </summary>
        public string Phase { get; set; }

        /// <summary>
        /// A brief CamelCase message indicating details about why the ClusterServiceVersion is in this state. e.g. 'RequirementsNotMet'
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// The status of each requirement for this CSV
        /// </summary>
        public List<RequirementStatus> RequirementStatus { get; set; }



    }
}

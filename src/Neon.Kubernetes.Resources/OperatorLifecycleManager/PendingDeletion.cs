// -----------------------------------------------------------------------------
// FILE:	    PendingDeletion.cs
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

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// PendingDeletion is the list of custom resource objects that are
    /// pending deletion and blocked on finalizers. This indicates the
    /// progress of cleanup that is blocking CSV deletion or operator uninstall.
    /// </summary>
    public class PendingDeletion
    {
        /// <summary>
        /// group
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// instances 
        /// </summary>
        public List<Instances> Instances { get; set; }

       /// <summary>
       /// kind
       /// </summary>
        public string Kind { get; set; }


    }
}

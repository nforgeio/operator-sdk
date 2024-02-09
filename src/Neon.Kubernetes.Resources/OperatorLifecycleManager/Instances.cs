// -----------------------------------------------------------------------------
// FILE:	    Instances.cs
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
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// Instances class
    /// </summary>
    public class Instances
    {
        /// <summary>
        /// name of the instance
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        ///Namespace can be empty for cluster-scoped resources
        /// </summary>
        public string Namespace { get; set; }
    }
}

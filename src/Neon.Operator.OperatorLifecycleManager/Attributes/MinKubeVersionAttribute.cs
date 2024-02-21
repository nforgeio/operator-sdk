// -----------------------------------------------------------------------------
// FILE:	    MinKubeVersionAttribute.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Specifies the minimum Kubernetes version required for the operator to run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class MinKubeVersionAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MinKubeVersionAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="minKubeVersion"></param>
        public MinKubeVersionAttribute(string minKubeVersion)
        {
            this.MinKubeVersion = minKubeVersion;
        }

        /// <summary>
        /// The minimum Kubernetes version.
        /// </summary>
        public string MinKubeVersion { get; set; }
    }
}

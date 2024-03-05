// -----------------------------------------------------------------------------
// FILE:	    UpdateGraphAttribute.cs
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
using System.Data;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Specifies the graph of the operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class UpdateGraphAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateGraphAttribute() { }

        /// <summary>
        /// Update graph 
        /// </summary>
        public UpdateGraphMode UpdateGraph { get; set; }
    }
    
}

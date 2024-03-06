// -----------------------------------------------------------------------------
// FILE:	    MaturityAttribute.cs
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
    /// Specifies the maturity of the Operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class MaturityAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MaturityAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maturity"></param>
        public MaturityAttribute(string maturity)
        {
            this.Maturity = maturity;
        }

        /// <summary>
        /// The maturity.
        /// </summary>
        public string Maturity { get; set; }
    }
}

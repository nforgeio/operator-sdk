// -----------------------------------------------------------------------------
// FILE:	    DescriptionAttribute.cs
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
    /// Specifies a description for an Operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class DescriptionAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DescriptionAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shortDescription"></param>
        /// <param name="fullDescription"></param>
        public DescriptionAttribute(string shortDescription, string fullDescription)
        {
            this.ShortDescription = shortDescription;
            this.FullDescription = fullDescription;
        }

        /// <summary>
        /// Short description of the operator.
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// Full description of the operator.
        /// </summary>
        public string FullDescription { get; set; }
    }
}

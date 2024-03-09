// -----------------------------------------------------------------------------
// FILE:	    MaintainerAttribute.cs
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
using System.ComponentModel;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Specifies a maintainer of the Operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class MaintainerAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MaintainerAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="gitHub"></param>
        /// <param name="reviewer"></param>
        public MaintainerAttribute(string name, string email, string gitHub, bool reviewer)
        {
            this.Name = name;
            this.Email = email;
            this.GitHub = gitHub;
            this.Reviewer = reviewer;
        }

        /// <summary>
        /// The maintainer name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The maintainer email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The maintainer Github username.
        /// </summary>
        public string GitHub { get; set; }

        /// <summary>
        /// reviewer flag for the maintainer.
        /// </summary>
        [DefaultValue(false)]
        public bool Reviewer { get; set; } = false;
    }
}

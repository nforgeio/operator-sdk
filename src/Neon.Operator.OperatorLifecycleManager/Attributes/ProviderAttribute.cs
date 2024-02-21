// -----------------------------------------------------------------------------
// FILE:	    ProviderAttribute.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Specifies the provider for the Operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class ProviderAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ProviderAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        public ProviderAttribute(string name, string url)
        {
            this.Name = name;
            this.Url = url;
        }

        /// <summary>
        /// The name of the provider.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The url for the provider.
        /// </summary>
        public string Url { get; set; }
    }
}

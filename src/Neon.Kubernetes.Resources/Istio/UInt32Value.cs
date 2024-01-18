//-----------------------------------------------------------------------------
// FILE:        UInt32Value.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using k8s;

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// <para>
    /// Wrapper message for uint32.
    ///
    /// The JSON representation for UInt32Value is JSON number.
    /// </para>
    /// </summary>
    public class UInt32Value : IValidate
    {
        /// <summary>
        /// Initializes a new instance of the UInt32Value class.
        /// </summary>
        public UInt32Value()
        {
        }

        /// <summary>
        /// The uint32 value.
        /// </summary>
        [DefaultValue(null)]
        public int? Value { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

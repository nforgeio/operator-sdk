//-----------------------------------------------------------------------------
// FILE:        Delegate.cs
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
    /// Describes the delegate V1VirtualService.
    /// </summary>
    public class Delegate : IValidate
    {
        /// <summary>
        /// Initializes a new instance of the Delegate class.
        /// </summary>
        public Delegate()
        {
        }

        /// <summary>
        /// Name specifies the name of the delegate V1VirtualService.
        /// </summary>
        [DefaultValue(null)]
        public string Name { get; set; }

        /// <summary>
        /// Namespace specifies the namespace where the delegate V1VirtualService resides. By default, it is same to the root’s.
        /// </summary>
        [DefaultValue(null)]
        public string Namespace { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

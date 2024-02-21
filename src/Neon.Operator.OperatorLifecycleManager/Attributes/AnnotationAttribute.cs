// -----------------------------------------------------------------------------
// FILE:	    AnnotationAttribute.cs
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
    /// An attribute that can be used to annotate an assembly with key-value pairs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class AnnotationAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AnnotationAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public AnnotationAttribute(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        /// <summary>
        /// The annotation key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The annotation value.
        /// </summary>
        public string Value { get; set; }
    }
}

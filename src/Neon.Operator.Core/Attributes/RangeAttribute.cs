// -----------------------------------------------------------------------------
// FILE:	    RangeAttribute.cs
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

namespace Neon.Operator.Attributes
{
    /// <summary>
    /// Specifies that the data must match the regex value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RangeAttribute : Attribute
    {
        /// <summary>
        /// The minimum value.
        /// </summary>
        public double Minimum
        {
            get
            {
                return minimum.GetValueOrDefault();
            }
            set
            {
                minimum = value;
            }
        }

        /// <summary>
        /// the maximum value.
        /// </summary>
        public double Maximum
        {
            get
            {
                return maximum.GetValueOrDefault();
            }
            set
            {
                maximum = value;
            }
        }

        /// <summary>
        /// Specifies whether the minimum is exclusive.
        /// </summary>
        public bool ExclusiveMinimum
        {
            get
            {
                return exclusiveMinimum.GetValueOrDefault();
            }
            set
            {
                exclusiveMinimum = value;
            }
        }

        /// <summary>
        /// Specifies whether the maximum is exclusive.
        /// </summary>
        public bool ExclusiveMaximum
        {
            get
            {
                return exclusiveMaximum.GetValueOrDefault();
            }
            set
            {
                exclusiveMaximum = value;
            }
        }

        internal double? minimum;
        internal double? maximum;
        internal bool? exclusiveMinimum;
        internal bool? exclusiveMaximum;
    }
}

// -----------------------------------------------------------------------------
// FILE:	    TypeNotRegisteredException.cs
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Operator.Xunit
{

    /// <summary>
    /// Represents an exception that is thrown when a type is not registered.
    /// </summary>
    public class TypeNotRegisteredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotRegisteredException"/> class with the specified group, version, and plural.
        /// </summary>
        /// <param name="group">The group of the type.</param>
        /// <param name="version">The version of the type.</param>
        /// <param name="plural">The plural form of the type.</param>
        public TypeNotRegisteredException(string group, string version, string plural)
            : base($"There is no type registered for {group}/{version}/{plural}. " +
                  $"Register the type by calling TestOperatorFixture.RegisterType<T>() in your unit test.")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotRegisteredException"/> class with the specified version, and plural.
        /// </summary>
        /// <param name="version">The version of the type.</param>
        /// <param name="plural">The plural form of the type.</param>
        public TypeNotRegisteredException(string version, string plural)
            : base($"There is no type registered for {version}/{plural}. " +
                  $"Register the type by calling TestOperatorFixture.RegisterType<T>() in your unit test.")
        {

        }
    }
}

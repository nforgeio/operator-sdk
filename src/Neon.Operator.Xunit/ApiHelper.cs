// -----------------------------------------------------------------------------
// FILE:	    ApiHelper.cs
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

using System.Text;

using Neon.Common;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Operator test helpers.
    /// </summary>
    internal static class ApiHelper
    {
        /// <summary>
        /// Creates a key by appending the argument strings passed, seperated
        /// by forward slashes.
        /// </summary>
        /// <param name="args">The argument strings.</param>
        /// <returns>The key string.</returns>
        public static string CreateKey(params string[] args)
        {
            var sb = new StringBuilder();

            foreach (var arg in args)
            {
                sb.AppendWithSeparator(arg, "/");
            }

            return sb.ToString();
        }
    }
}

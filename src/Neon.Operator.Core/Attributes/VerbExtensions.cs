//-----------------------------------------------------------------------------
// FILE:	    VerbExtensions.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neon.Operator.Rbac
{
    /// <summary>
    /// RBAC verb extension methods.
    /// </summary>
    public static class VerbExtensions
    {
        /// <summary>
        /// Converts an <see cref="RbacVerb"/> into a list of corresponding strings.
        /// </summary>
        /// <param name="verb">Specifies the RBAC verb.</param>
        /// <returns></returns>
        public static IList<string> ToStrings(this RbacVerb verb)
        {
            var result = new List<string>();

            if (verb == RbacVerb.None)
            {
                return result;
            }

            if (verb.HasFlag(RbacVerb.All))
            {
                result.Add("*");
                return result;
            }

            var enumValues = Enum.GetValues(typeof(RbacVerb));

            for (int i = 0; i < enumValues.Length; i++)
            {
                var verbValue = (RbacVerb)enumValues.GetValue(i);

                if (verbValue == RbacVerb.None ||  verbValue == RbacVerb.All)
                {
                    continue;
                }

                if (verb.HasFlag(verbValue))
                {
                    result.Add(verbValue.ToString().ToLower());
                }
            }


            return result.OrderBy(value => value).ToList();
        }
    }
}
// -----------------------------------------------------------------------------
// FILE:	    Extensions.cs
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

using Neon.BuildInfo;
using Neon.Common;
using Neon.Operator.Rbac;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Useful extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts the <see cref="Category"/> to a list of strings.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static List<string> ToStrings(this Category category)
        {
            var result = new List<string>();

            var enumValues = Enum.GetValues(typeof(Category));

            for (int i = 0; i < enumValues.Length; i++)
            {
                var categoryValue = (Category)enumValues.GetValue(i);

                if (category.HasFlag(categoryValue))
                {
                    result.Add(categoryValue.ToMemberString());
                }
            }

            return result.OrderBy(value => value).ToList();
        }

        /// <summary>
        /// Gets the types of the <see cref="InstallModeType"/>.
        /// </summary>
        /// <param name="installMode"></param>
        /// <returns></returns>
        public static List<InstallModeType> GetTypes(this InstallModeType installMode)
        {
            var result = new List<InstallModeType>();

            var enumValues = Enum.GetValues(typeof(InstallModeType));

            for (int i = 0; i < enumValues.Length; i++)
            {
                var installModeValue = (InstallModeType)enumValues.GetValue(i);

                if (installMode.HasFlag(installModeValue))
                {
                    result.Add(installModeValue);
                }
            }

            return result.OrderBy(value => value.ToMemberString()).ToList();
        }
    }
}

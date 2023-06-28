// -----------------------------------------------------------------------------
// FILE:	    TypeExtensions.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Roslyn.Reflection;

namespace Neon.Operator.Analyzers
{
    public static class TypeExtensions
    {
        public static bool IsSimpleType(this Type type)
        {
            if (type.IsPrimitive)
            {
                return true;
            }

            if (type.Equals(typeof(string))
                || type.Equals(typeof(Guid))
                || type.Equals(typeof(TimeSpan))
                || type.Equals(typeof(DateTime))
                || type.Equals(typeof(DateTimeOffset))
                )
            {
                return true;
            }

            if (type.IsGenericType
                && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (type.GetGenericArguments().FirstOrDefault().IsSimpleType())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEnumerableType(this Type type, out Type typeParameter)
        {
            var genericDef = (RoslynType)type.GetGenericTypeDefinition();
            
            if (genericDef.IsAssignableTo(typeof(IEnumerable<>))
                || genericDef.IsAssignableTo(typeof(IList<>)))
            {
                typeParameter = type.GetGenericArguments().First();
            }
            else
            {
                typeParameter = type
                    .GetInterfaces()
                    .Where(t => t.IsGenericType
                            && (t.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>))
                                || t.GetGenericTypeDefinition().Equals(typeof(IList<>))))
                    .Select(t => t.GetGenericArguments().FirstOrDefault())
                    .FirstOrDefault();
            }

            if (typeParameter != null)
            {
                return true;
            }

            return false;
        }
    }
}

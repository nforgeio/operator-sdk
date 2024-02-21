// -----------------------------------------------------------------------------
// FILE:	    KeywordAttribute.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Specifies a keyword for the Operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class KeywordAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public KeywordAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="keyword"></param>
        public KeywordAttribute(string keyword)
        {
            this.Keyword = keyword;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeywordAttribute(params string[] keywords)
        {
            this.Keywords = keywords.ToList();
        }

        /// <summary>
        /// A keyword.
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// The keywords.
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// Gets the keywords as a list.
        /// </summary>
        /// <returns></returns>
        public List<string> GetKeywords()
        {
            if (!string.IsNullOrEmpty(Keyword))
            {
                Keywords.Add(Keyword);
            }

            return Keywords;
        }
    }
}

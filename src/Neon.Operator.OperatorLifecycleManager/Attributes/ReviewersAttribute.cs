// -----------------------------------------------------------------------------
// FILE:	    ReviewersAttribute.cs
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
    /// A list of reviewers to be added to pull requests (GitHub user name)
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class ReviewersAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ReviewersAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reviewer"></param>
        public ReviewersAttribute(string reviewer)
        {
            this.Reviewer = reviewer;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ReviewersAttribute(params string[] reviewers)
        {
            this.Reviewers = reviewers.ToList();
        }

        /// <summary>
        /// A reviewer.
        /// </summary>
        public string Reviewer { get; set; }

        /// <summary>
        /// The reviewers.
        /// </summary>
        public List<string> Reviewers { get; set; } = new List<string>();

        /// <summary>
        /// Gets the reviewers as a list.
        /// </summary>
        /// <returns></returns>
        public List<string> GetReviewers()
        {
            if (!string.IsNullOrEmpty(Reviewer))
            {
                Reviewers.Add(Reviewer);
            }

            return Reviewers;
        }
    }
}

//-----------------------------------------------------------------------------
// FILE:	    RbacRuleAttribute.cs
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
using System.Reflection;

using k8s;
using k8s.Models;

using Neon.Operator.Rbac;

namespace Neon.Operator.Attributes
{
    /// <summary>
    /// Used to exclude a component from assembly scanning when building the operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RbacRuleAttribute : Attribute, IRbacRule
    {
        /// <inheritdoc/>
        public string ApiGroup { get; set; }

        /// <inheritdoc/>
        public string Resource { get; set; }
        /// <summary>
        /// The list of verbs describing the allowed actions.
        /// </summary>
        public RbacVerb Verbs { get; set; } = RbacVerb.None;

        /// <summary>
        /// The <see cref="EntityScope"/> of the permission.
        /// </summary>
        public EntityScope Scope { get; set; } = EntityScope.Namespaced;

        /// <summary>
        /// Comma separated list of namespaces to watch. 
        /// </summary>
        public string Namespace { get; set; } = null;

        /// <summary>
        /// Comma separated list of resource names. When specified, requests can be restricted to individual 
        /// instances of a resource
        /// </summary>
        public string ResourceNames { get; set; } = null;

        /// <summary>
        /// Comma separated list of subresources.
        /// </summary>
        public string SubResources { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public RbacRuleAttribute()
        {
           
        }

        /// <inheritdoc/>
        public IEnumerable<string> NamespaceList()
        {
            return Namespace?.Split(',') ?? null;
        }
    }
}

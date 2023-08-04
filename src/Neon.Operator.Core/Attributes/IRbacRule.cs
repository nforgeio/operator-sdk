//-----------------------------------------------------------------------------
// FILE:	    IRbacRule.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

using k8s.Models;

using Neon.Operator.Attributes;

namespace Neon.Operator.Rbac
{
    /// <summary>
    /// Describes an RBAC rule.
    /// </summary>
    public interface IRbacRule
    {
        /// <summary>
        /// The Kubernetes API group.
        /// </summary>
        public string ApiGroup { get; }

        /// <summary>
        /// The Resource type name.
        /// </summary>
        public string Resource { get; }

        /// <summary>
        /// Verbs describe what operations the client can perform on the resource.
        /// </summary>
        RbacVerb Verbs { get; set; }

        /// <summary>
        /// The scope of the rule. This is either Namespaced or Cluster.
        /// </summary>
        EntityScope Scope { get; set; }

        /// <summary>
        /// Optional comma-separated list of specific resources to apply the rule to.
        /// </summary>
        string ResourceNames { get; set; }

        /// <summary>
        /// Optional comma-separated list of subresources.
        /// </summary>
        string SubResources { get; set; }

        /// <summary>
        /// Optional comma-separated list of namespaces.
        /// </summary>
        string Namespace { get; set; }

        /// <summary>
        /// Gets a list of the Namespaces that the rule applies to.
        /// </summary>
        /// <returns>The list of namespaces.</returns>
        IEnumerable<string> NamespaceList();
    }
}

// -----------------------------------------------------------------------------
// FILE:	    PolicyRule.cs
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
using System.Drawing;
using System.Text;
using System.Xml.Linq;
using k8s.Models;
using OpenTelemetry.Resources;

using static Neon.K8s.Resources.Istio.AuthorizationPolicyRule;

using YamlDotNet.Core;
using System.Data;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    public class PolicyRule
    {
        /// <summary>
        /// APIGroups is the name of the APIGroup that contains the resources.
        /// If multiple API groups are specified, any action requested against one
        /// of the enumerated resources in any API group will be allowed. ""
        /// represents the core API group and "*" represents all API groups.
        /// </summary>
        public List<string> APIGroups { get; set; }

        /// <summary>
        /// NonResourceURLs is a set of partial urls that a user should have access
        /// to. *s are allowed, but only as the full, final step in the path Since
        /// non-resource URLs are not namespaced, this field is only applicable for
        /// ClusterRoles referenced from a ClusterRoleBinding.Rules can either apply
        /// to API resources (such as "pods" or "secrets") or non-resource URL paths
        /// (such as "/api"), but not both.
        /// </summary>
        public List<string> NonResourceURLs {  get; set; }

        /// <summary>
        /// ResourceNames is an optional white list of names that the rule applies to.
        /// An empty set means that everything is allowed.
        /// </summary>
        public List<string> ResourceNames { get; set; }

        /// <summary>
        ///  Resources is a list of resources this rule applies to. '*' represents all resources.
        /// </summary>
        public List<string> Resources { get; set; }

        /// <summary>
        /// Verbs is a list of Verbs that apply to ALL the ResourceKinds contained in this rule. '*' represents all verbs.
        /// </summary>
        public List<string> Verbs { get; set; }

    }
}

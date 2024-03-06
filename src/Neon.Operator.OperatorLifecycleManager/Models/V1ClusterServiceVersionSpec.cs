// -----------------------------------------------------------------------------
// FILE:	    V1ClusterServiceVersionSpec.cs
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

using System.Collections.Generic;

using k8s.Models;

using YamlDotNet.Serialization;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// ClusterServiceVersionSpec declarations tell OLM how to install an operator that can manage apps for a given version.
    /// </summary>
    public class V1ClusterServiceVersionSpec
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public V1ClusterServiceVersionSpec()
        {
        }

        /// <summary>
        /// Annotations is an unstructured key value map stored with a resource
        /// that may be set by external tools to store and retrieve arbitrary metadata.
        /// </summary>
        public Dictionary<string, string> Annotations { get; set; }

        /// <summary>
        /// ApiServiceDefinitions declares all of the extension apis managed or required by an operator
        /// being ran by ClusterServiceVersion.
        /// </summary>
        [YamlMember(Alias = "apiservicedefinitions", ApplyNamingConventions = false)]
        public ApiServiceDefinitions ApiServiceDefinitions { get; set; }

        /// <summary>
        /// Cleanup specifies the cleanup behaviour when the CSV gets deleted
        /// </summary>
        public Cleanup Cleanup { get; set; }

        /// <summary>
        /// CustomResourceDefinitions declares all of the CRDs managed or required by an operator being
        /// ran by ClusterServiceVersion. If the CRD is present in the Owned list, it is implicitly required.
        /// </summary>
        [YamlMember(Alias = "customresourcedefinitions", ApplyNamingConventions = false)]
        public CustomResourceDefinitions CustomResourceDefinitions { get; set; }

        /// <summary>
        /// Description of the operator. Can include the features, limitations or use-cases of the operator.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The name of the operator in display format.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Icon for this operator
        /// </summary>
        public List<Icon> Icon { get; set; }

        /// <summary>
        /// NamedInstallStrategy represents the block of an ClusterServiceVersion resource where the install strategy is specified.
        /// </summary>
        public NamedInstallStrategy Install { get; set; }

        /// <summary>
        /// Type associates an InstallModeType with a flag representing if the CSV supports it
        /// </summary>
        public List<InstallMode> InstallModes { get; set; }

        /// <summary>
        /// A list of keywords describing the operator.
        /// </summary>
        public List<string> Keywords { get; set; }

        /// <summary>
        /// Map of string keys and values that can be used to organize and categorize(scope and select) objects.
        /// </summary>
        public Dictionary<string, string> Labels { get; set; }

        /// <summary>
        /// A list of links related to the operator.
        /// </summary>
        public List<Link> Links { get; set; }

        /// <summary>
        /// A list of organizational entities maintaining the operator.
        /// </summary>
        public List<Maintainer> Maintainers { get; set; }

        /// <summary>
        /// Maturity of the operator.
        /// </summary>
        public string Maturity { get; set; }

        /// <summary>
        /// MinKubeVersion is the minimum version of the Kubernetes to run the operator
        /// </summary>
        public string MinKubeVersion { get; set; }

        /// <summary>
        ///  GroupVersionKind unambiguously identifies a kind. It doesn’t anonymously
        ///  include GroupVersion to avoid automatic coercion.It doesn’t use a GroupVersion to avoid custom marshalling
        /// </summary>
        [YamlMember(Alias = "nativeAPIs", ApplyNamingConventions = false)]
        public List<GroupVersionKind> NativeApis { get; set; }

        /// <summary>
        /// The publishing entity behind the operator.
        /// </summary>
        public Provider Provider { get; set; }

        /// <summary>
        /// List any related images, or other container images that your Operator might require to perform their functions.
        /// This list should also include operand images as well. All image references should be specified
        /// by digest (SHA) and not by tag. This field is only used during catalog creation and plays no part in cluster runtime.
        /// </summary>
        public List<RelatedImages> RelatedImages { get; set; }

        /// <summary>
        /// The name of a CSV this one replaces. Should match the metadata.Name field of the old CSV.
        /// </summary>
        public string Replaces { get; set; }

        /// <summary>
        /// Label selector for related resources.
        /// </summary>
        public V1LabelSelector Selector { get; set; }

        /// <summary>
        /// The name(s) of one or more CSV(s) that should be skipped in the upgrade graph. Should match the metadata.
        /// Name field of the CSV that should be skipped. This field is only used during catalog creation and plays no
        /// spart in cluster runtime.
        /// </summary>
        public List<string> Skips { get; set; }

        /// <summary>
        /// version of the operator
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///  WebhookDescription provides details to OLM about required webhooks
        /// </summary>
        public List<WebHookDescription> WebHookDefinitions {get;set;}
    }
}

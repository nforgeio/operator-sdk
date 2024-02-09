// -----------------------------------------------------------------------------
// FILE:	    WebHookDescription.cs
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

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// WebhookDescription provides details to OLM about required webhooks
    /// </summary>
    public class WebHookDescription
    {
        
        /// <summary>
        /// AdmissionReviewVersions
        /// </summary>
        public List<string> AdmissionReviewVersions { get; set; }

        /// <summary>
        /// ContainerPort
        /// </summary>
        public int ContainerPort {  get; set; }

        /// <summary>
        /// ConversionCRDs
        /// </summary>
        [YamlMember(Alias = "conversionCRDs", ApplyNamingConventions = false)]
        public List<string> ConversionCrds { get; set; }

        /// <summary>
        /// DeploymentName
        /// </summary>
        public string DeploymentName {  get; set; }

        /// <summary>
        /// FailurePolicyType specifies a failure policy
        /// that defines how unrecognized errors from the admission endpoint are handled.
        /// </summary>
        public string FailurePolicy {  get; set; }

        /// <summary>
        /// GenerateName
        /// </summary>
        public string GenerateName {  get; set; }

        /// <summary>
        /// MatchPolicyType specifies the type of match policy.
        /// </summary>
        public string MatchPolicy {  get; set; }

        /// <summary>
        /// A label selector is a label query over a set of resources.
        /// The result of matchLabels and matchExpressions are ANDed.
        /// An empty label selector matches all objects.A null label selector matches no objects.
        /// </summary>
        public V1LabelSelector ObjectSelector { get; set; }

        /// <summary>
        /// ReinvocationPolicyType specifies what type of policy the admission hook uses.
        /// </summary>
        public string ReinvocationPolicy {  get; set; }

        /// <summary>
        /// RuleWithOperations is a tuple of Operations and Resources.It is recommended
        /// to make sure that all the tuple expansions are valid.
        /// </summary>
        public List<V1RuleWithOperations> Rules {  get; set; }

        /// <summary>
        /// SideEffectClass specifies the types of side effects a webhook may have.
        /// </summary>
        public string SideEffects {  get; set; }

        /// <summary>
        /// Target port
        /// </summary>
        public int TargetPort {  get; set; }

        /// <summary>
        /// TimeoutSeconds
        /// </summary>
        public int TimeoutSeconds {  get; set; }

        /// <summary>
        /// WebhookAdmissionType is the type of admission webhooks supported by OLM
        /// </summary>
        public string Type {  get; set; }

        /// <summary>
        /// web hook path
        /// </summary>
        public string WebHookPath {  get; set; }
    }
}

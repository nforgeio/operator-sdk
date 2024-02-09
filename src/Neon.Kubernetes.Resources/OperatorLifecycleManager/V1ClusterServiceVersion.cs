// -----------------------------------------------------------------------------
// FILE:	    V1ClusterServiceVersion.cs
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

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// ClusterServiceVersion.
    /// </summary>
    [KubernetesEntity(Group = KubeGroup, Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    [Ignore]
    public class V1ClusterServiceVersion : IKubernetesObject<V1ObjectMeta>, ISpec<V1ClusterServiceVersionSpec>, IStatus<V1ClusterServiceVersionStatus>
    {
        
        /// <summary>
        /// The API version this Kubernetes type belongs to.
        /// </summary>
        public const string KubeApiVersion = "v1alpha1";

        /// <summary>
        /// The Kubernetes named schema this object is based on.
        /// </summary>
        public const string KubeKind = "ClusterServiceVersion";

        /// <summary>
        /// The Group this Kubernetes type belongs to.
        /// </summary>
        public const string KubeGroup = "operators.coreos.com";

        /// <summary>
        /// The plural name of the entity.
        /// </summary>
        public const string KubePlural = "clusterserviceversions";

        /// <summary>
        /// Initializes a new instance of the V1ClusterServiceVersion class.
        /// </summary>
        public V1ClusterServiceVersion()
        {
            ApiVersion = $"{KubeGroup}/{KubeApiVersion}";
            Kind = KubeKind;
        }

        /// <summary>
        /// Gets or sets APIVersion defines the versioned schema of this
        /// representation of an object. Servers should convert recognized
        /// schemas to the latest internal value, and may reject unrecognized
        /// values. More info:
        /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#resources
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets kind is a string value representing the REST resource
        /// this object represents. Servers may infer this from the endpoint
        /// the client submits requests to. Cannot be updated. In CamelCase.
        /// More info:
        /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#types-kinds
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets standard object metadata.
        /// </summary>
        public V1ObjectMeta Metadata { get; set; }

        /// <summary>
        /// Gets or sets specification of the desired behavior of the
        /// ServiceMonitor.
        /// </summary>
        public V1ClusterServiceVersionSpec Spec { get; set; }

        /// <summary>
        /// ClusterServiceVersionStatus represents information about the status of a CSV. Status may trail the actual state of a system.
        /// </summary>
        public V1ClusterServiceVersionStatus Status { get; set; }
    }
}

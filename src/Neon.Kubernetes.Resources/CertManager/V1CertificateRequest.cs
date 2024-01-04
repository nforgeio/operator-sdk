//-----------------------------------------------------------------------------
// FILE:        V1CertificateRequest.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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

using System.ComponentModel.DataAnnotations;

using k8s;
using k8s.Models;

using Neon.JsonConverters;
using Neon.Operator.Attributes;

using Newtonsoft.Json;

namespace Neon.K8s.Resources.CertManager
{
    /// <summary>
    /// A CertificateRequest is used to request a signed certificate from one of the configured issuers. 
    /// All fields within the CertificateRequest's `spec` are immutable after creation. A CertificateRequest 
    /// will either succeed or fail, as denoted by its `status.state` field. A CertificateRequest is a
    /// one-shot resource, meaning it represents a single point in time request for a certificate and cannot 
    /// be re-used.
    /// </summary>
    [KubernetesEntity(Group = KubeGroup, Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    [Ignore]
    public class V1CertificateRequest : IKubernetesObject<V1ObjectMeta>, ISpec<object>, IValidate
    {
        /// <summary>
        /// The API version this Kubernetes type belongs to.
        /// </summary>
        public const string KubeApiVersion = "v1alpha2";

        /// <summary>
        /// The Kubernetes named schema this object is based on.
        /// </summary>
        public const string KubeKind = "CertificateRequest";

        /// <summary>
        /// The Group this Kubernetes type belongs to.
        /// </summary>
        public const string KubeGroup = "cert-manager.io";

        /// <summary>
        /// The plural name of the entity.
        /// </summary>
        public const string KubePlural = "certificaterequests";

        /// <summary>
        /// Initializes a new instance of the CertificateRequest class.
        /// </summary>
        /// 
        public V1CertificateRequest()
        {
            ApiVersion = $"{KubeGroup}/{KubeApiVersion}";
            Kind       = KubeKind;
        }

        /// <summary>
        /// Gets or sets APIVersion defines the versioned schema of this
        /// representation of an object. Servers should convert recognized
        /// schemas to the latest internal value, and may reject unrecognized
        /// values. More info:
        /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#resources
        /// </summary>
        [JsonProperty(PropertyName = "apiVersion")]
        public string ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets kind is a string value representing the REST resource
        /// this object represents. Servers may infer this from the endpoint
        /// the client submits requests to. Cannot be updated. In CamelCase.
        /// More info:
        /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#types-kinds
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets standard object metadata.
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public V1ObjectMeta Metadata { get; set; }

        /// <summary>
        /// Gets or sets specification of the desired behavior of the
        /// CertificateRequest.
        /// </summary>
        [JsonProperty(PropertyName = "spec")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonGenericConverter<dynamic>))]
        public dynamic Spec { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

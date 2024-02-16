using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

namespace Test_Analyzers.Models
{
    [KubernetesEntity(Group = KubeGroup, Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    [EntityVersion(Served = true, Storage = false)]
    [ShortName("ex")]
    public class V1GenericEntity<T> : IKubernetesObject<V1ObjectMeta>, ISpec<V1GenericEntity<T>.V1GenericSpec>
    {
        /// <summary>
        /// The API version this Kubernetes type belongs to.
        /// </summary>
        public const string KubeApiVersion = "v1alpha1";

        /// <summary>
        /// The Kubernetes named schema this object is based on.
        /// </summary>
        public const string KubeKind = "Generic";

        /// <summary>
        /// The Group this Kubernetes type belongs to.
        /// </summary>
        public const string KubeGroup = "example.neonkube.io";

        /// <summary>
        /// The plural name of the entity.
        /// </summary>
        public const string KubePlural = "generics";

        /// <summary>
        /// Constructor.
        /// </summary>
        public V1GenericEntity()
        {
            ApiVersion = $"{KubeGroup}/{KubeApiVersion}";
            Kind = KubeKind;
        }

        /// <inheritdoc/>
        public string ApiVersion { get; set; }

        /// <inheritdoc/>
        public string Kind { get; set; }

        /// <inheritdoc/>
        public V1ObjectMeta Metadata { get; set; }

        /// <summary>
        /// This is the description for the spec.
        /// </summary>
        public V1GenericSpec Spec { get; set; }

        /// <summary>
        /// The Value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class V1GenericSpec
        {
            public T Value { get; set; }
        }
    }
}

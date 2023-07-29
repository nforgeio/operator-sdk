using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

using RangeAttribute = Neon.Operator.Attributes.RangeAttribute;

namespace TestOperator
{
    /// <summary>
    /// This is an example description.
    /// </summary>
    [KubernetesEntity(Group = KubeGroup, Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    [EntityVersion(Served = true, Storage = false)]
    [ShortName("ex")]
    public class V1ExampleEntity : IKubernetesObject<V1ObjectMeta>, ISpec<V1ExampleEntity.V1ExampleSpec>, IStatus<V1ExampleEntity.V1ExampleStatus>
    {
        /// <summary>
        /// The API version this Kubernetes type belongs to.
        /// </summary>
        public const string KubeApiVersion = "v1alpha1";

        /// <summary>
        /// The Kubernetes named schema this object is based on.
        /// </summary>
        public const string KubeKind = "Example";

        /// <summary>
        /// The Group this Kubernetes type belongs to.
        /// </summary>
        public const string KubeGroup = "example.neonkube.io";

        /// <summary>
        /// The plural name of the entity.
        /// </summary>
        public const string KubePlural = "examples";

        /// <summary>
        /// Constructor.
        /// </summary>
        public V1ExampleEntity()
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
        public V1ExampleSpec Spec { get; set; }
        /// <inheritdoc/>
        public V1ExampleStatus Status { get; set; }

        public class V1ExampleSpec
        {
            [Pattern("^(\\d+|\\*)(/\\d+)?(\\s+(\\d+|\\*)(/\\d+)?){4}$")]
            public string Message { get; set; }

            [AdditionalPrinterColumn]
            [Range(Minimum = 0.0, ExclusiveMinimum = true)]
            public int? Count { get; set; }

            public Person Person { get; set; }
        }

        public class V1ExampleStatus
        {
            public string Message { get; set; }
        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public List<string> Nicknames { get; set; }
            public IEnumerable<string> Foo { get; set; }

            [PreserveUnknownFields]
            public Dictionary<string, object> Bar { get; set; }
            public KeyValuePair<string, object> Baz { get; set; }
        }
    }
}

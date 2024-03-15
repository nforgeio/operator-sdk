using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

using RangeAttribute = Neon.Operator.Attributes.RangeAttribute;

namespace Test_Analyzers
{
    /// <summary>
    /// This is an example description. A <see cref="V3ExampleEntity"/> is a <see cref="IKubernetesObject{V3ObjectMeta}"/>
    /// with a <see cref="V3ExampleEntity.V3ExampleSpec"/> and a <see cref="V3ExampleEntity.V3ExampleStatus"/>.
    /// </summary>
    [KubernetesEntity(Group = "example.neonkube.io", Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    [EntityVersion(Served = true, Storage = true)]
    [ShortName("ex")]
    [Ignore]
    public class V3ExampleEntity : IKubernetesObject<V3ObjectMeta>, ISpec<V3ExampleEntity.V3ExampleSpec>, IStatus<V3ExampleEntity.V3ExampleStatus>
    {
        /// <summary>
        /// The API version this Kubernetes type belongs to.
        /// </summary>
        public const string KubeApiVersion = "V3alpha1";

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
        public V3ExampleEntity()
        {
            ApiVersion = $"{KubeGroup}/{KubeApiVersion}";
            Kind = KubeKind;
        }

        /// <inheritdoc/>
        public string ApiVersion { get; set; }

        /// <inheritdoc/>
        public string Kind { get; set; }

        /// <inheritdoc/>
        public V3ObjectMeta Metadata { get; set; }

        /// <summary>
        /// This is the description for the spec.
        /// </summary>
        public V3ExampleSpec Spec { get; set; }

        /// <inheritdoc/>
        public V3ExampleStatus Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class V3ExampleSpec
        {
            /// <summary>
            /// The message
            /// </summary>
            [Pattern("^(\\d+|\\*)(/\\d+)?(\\s+(\\d+|\\*)(/\\d+)?){4}$")]
            [DefaultValue("default message")]
            public string Message { get; set; }

            /// <summary>
            /// The count.
            /// </summary>
            [AdditionalPrinterColumn]
            [Range(Minimum = 0.0, ExclusiveMinimum = true)]
            public int? Count { get; set; }

            /// <summary>
            /// The <see cref="V3ExampleEntity.Person"/>
            /// </summary>
            public Person Person { get; set; }

            /// <summary>
            /// Indicates whether this job is enabled or disabled.  This defaults to <c>false</c>.
            /// </summary>
            [DefaultValue(false)]
            public bool Enabled { get; set; } = false;
        }

        /// <summary>
        /// The status.
        /// </summary>
        public class V3ExampleStatus
        {
            /// <summary>
            /// Status message.
            /// </summary>
            public string Message { get; set; }
        }

        /// <summary>
        /// This is a person. It is also a <see cref="IPerson"/>
        /// </summary>
        public class Person
        {
            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            [Required]
            public string Name { get; set; }

            /// <summary>
            /// <inheritdoc/>
            /// Some additional comments
            /// </summary>
            public int Age { get; set; }

            /// <summary>
            /// A list of nicknames for the person.
            /// </summary>
            public List<string> Nicknames { get; set; }

            /// <summary>
            /// Foo
            /// </summary>
            public IEnumerable<string> Foo { get; set; }

            /// <summary>
            /// Bar
            /// </summary>
            [PreserveUnknownFields]
            public Dictionary<string, object> Bar { get; set; }

            /// <summary>
            /// Baz
            /// </summary>
            public KeyValuePair<string, object> Baz { get; set; }

            /// <summary>
            /// Gender
            /// </summary>
            public Gender Gender { get; set; }

            /// <summary>
            /// This is nullable.
            /// </summary>
            public NullEnum? FooEnum { get; set; }
        }

        /// <summary>
        /// This is an <see cref="IPerson"/>
        /// </summary>
        public interface IPerson
        {
            /// <summary>
            /// The name of the person.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The age of the person.
            /// </summary>
            public string Age { get; set; }
        }

        /// <summary>
        /// Genders
        /// </summary>
        public enum Gender
        {
            /// <summary>
            /// Male
            /// </summary>
            Male,

            /// <summary>
            /// Female
            /// </summary>
            Female,

            /// <summary>
            /// Other
            /// </summary>
            Other
        }

        /// <summary>
        /// This enum will be nullable
        /// </summary>
        public enum NullEnum
        {
            /// <summary>
            /// Foo
            /// </summary>
            Foo
        }
    }
}

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
    /// This is an example description. A <see cref="ExampleDictEntity"/> is a <see cref="IKubernetesObject{V1ObjectMeta}"/>
    /// with a <see cref="ExampleDictEntity.V1ExampleSpec"/> and a <see cref="ExampleDictEntity.V1ExampleStatus"/>.
    /// </summary>
    [KubernetesEntity(Group = "example.neonkube.io", Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    [EntityVersion(Served = true, Storage = true)]
    [ShortName("ex")]
    public class ExampleDictEntity : IKubernetesObject<V1ObjectMeta>, ISpec<ExampleDictEntity.V1ExampleSpec>, IStatus<ExampleDictEntity.V1ExampleStatus>
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
        public const string KubePlural = "dict";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExampleDictEntity()
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class V1ExampleSpec
        {
            public Dictionary<string, V1Condition> Conditions { get; set; }
        }
    }
}

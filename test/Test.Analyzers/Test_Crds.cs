using System;
using System.IO;
using System.Collections.Generic;

using FluentAssertions;

using k8s.Models;

using Neon.IO;
using Neon.Operator.Analyzers;
using Neon.Operator.Attributes;

using Xunit.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Test.Analyzers
{
    public class Test_Crds
    {
        private readonly ITestOutputHelper output;
        public Test_Crds(ITestOutputHelper output)
        {
            this.output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void TestGenerateCrd()
        {
            var entityDefinition = @"
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

using RangeAttribute = Neon.Operator.Attributes.RangeAttribute;

namespace TestOperator
{
    /// <summary>
    /// This is an example description. A <see cref=""V1ExampleEntity""/> is a <see cref=""IKubernetesObject{V1ObjectMeta}""/>
    /// with a <see cref=""V1ExampleEntity.V1ExampleSpec""/> and a <see cref=""V1ExampleEntity.V1ExampleStatus""/>.
    /// </summary>
    [KubernetesEntity(Group = KubeGroup, Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    [EntityVersion(Served = true, Storage = false)]
    [ShortName(""ex"")]
    public class V1ExampleEntity : IKubernetesObject<V1ObjectMeta>, ISpec<V1ExampleEntity.V1ExampleSpec>, IStatus<V1ExampleEntity.V1ExampleStatus>
    {
        /// <summary>
        /// The API version this Kubernetes type belongs to.
        /// </summary>
        public const string KubeApiVersion = ""v1alpha1"";

        /// <summary>
        /// The Kubernetes named schema this object is based on.
        /// </summary>
        public const string KubeKind = ""Example"";

        /// <summary>
        /// The Group this Kubernetes type belongs to.
        /// </summary>
        public const string KubeGroup = ""example.neonkube.io"";

        /// <summary>
        /// The plural name of the entity.
        /// </summary>
        public const string KubePlural = ""examples"";

        /// <summary>
        /// Constructor.
        /// </summary>
        public V1ExampleEntity()
        {
            ApiVersion = $""{KubeGroup}/{KubeApiVersion}"";
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

        /// <summary>
        /// An example spec.
        /// </summary>
        public class V1ExampleSpec
        {
            /// <summary>
            /// The message
            /// </summary>
            [Pattern(""^(\\d+|\\*)(/\\d+)?(\\s+(\\d+|\\*)(/\\d+)?){4}$"")]
            public string Message { get; set; }

            /// <summary>
            /// The count.
            /// </summary>
            [AdditionalPrinterColumn]
            [Range(Minimum = 0.0, ExclusiveMinimum = true)]
            public int? Count { get; set; }

            /// <summary>
            /// The <see cref=""V1ExampleEntity.Person""/>
            /// </summary>
            public Person Person { get; set; }
        }

        /// <summary>
        /// The status.
        /// </summary>
        public class V1ExampleStatus
        {
            /// <summary>
            /// Status message.
            /// </summary>
            public string Message { get; set; }
        }

        /// <summary>
        /// This is a person. It is also a <see cref=""IPerson""/>
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
        /// This is an <see cref=""IPerson""/>
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
}";

            var expectedCrd = @"apiVersion: apiextensions.k8s.io/v1
kind: CustomResourceDefinition
metadata:
  name: examples.example.neonkube.io
spec:
  group: example.neonkube.io
  names:
    kind: Example
    plural: examples
    shortNames:
    - ex
    singular: example
  scope: Namespaced
  versions:
  - additionalPrinterColumns:
    - description: The count.
      format: int32
      jsonPath: .spec.count
      name: Count
      priority: 0
      type: integer
    name: v1alpha1
    schema:
      openAPIV3Schema:
        description: This is an example description. A TestOperator.V1ExampleEntity  is a k8s.IKubernetesObject`1  with a TestOperator.V1ExampleEntity.V1ExampleSpec  and a TestOperator.V1ExampleEntity.V1ExampleStatus .
        properties:
          spec:
            description: This is the description for the spec.
            properties:
              message:
                description: The message
                pattern: ^(\d+|\*)(/\d+)?(\s+(\d+|\*)(/\d+)?){4}$
                type: string
              count:
                description: The count.
                exclusiveMinimum: true
                format: int32
                minimum: 0.0
                type: integer
              person:
                description: The TestOperator.V1ExampleEntity.Person
                properties:
                  name:
                    type: string
                  age:
                    description: Some additional comments
                    format: int32
                    type: integer
                  nicknames:
                    description: A list of nicknames for the person.
                    items:
                      type: string
                    type: array
                  foo:
                    description: Foo
                    items:
                      type: string
                    type: array
                  bar:
                    description: Bar
                    items:
                      properties:
                        key:
                          type: string
                        value:
                          properties: {}
                          type: object
                      type: object
                    type: array
                    x-kubernetes-preserve-unknown-fields: true
                  baz:
                    description: Baz
                    properties:
                      key:
                        type: string
                      value:
                        properties: {}
                        type: object
                    type: object
                  gender:
                    description: Gender
                    enum:
                    - male
                    - female
                    - other
                    type: string
                  fooEnum:
                    description: This is nullable.
                    enum:
                    - foo
                    type: string
                required:
                - name
                type: object
            type: object
          status:
            properties:
              message:
                description: Status message.
                type: string
            type: object
        type: object
    served: true
    storage: false
    subresources:
      status: {}";

            using var tempFile = new TempFolder();

            var optionsProvider = new OperatorAnalyzerConfigOptionsProvider();
            optionsProvider.SetOptions(new OperatorAnalyzerConfigOptions()
            {
                Options = new Dictionary<string, string>()
                {
                    {"build_property.NeonOperatorGenerateCrds", "true" },
                    {"build_property.NeonOperatorCrdOutputDir", tempFile.Path }
                }
            });

            var generatedCode = CompilationHelper.GetGeneratedOutput<CustomResourceDefinitionGenerator>(
                source: entityDefinition,
                additionalAssemblies: [
                    typeof(KubernetesEntityAttribute).Assembly,
                    typeof(AdditionalPrinterColumnAttribute).Assembly,
                    typeof(V1Pod).Assembly,
                    typeof(RequiredAttribute).Assembly,
                ],
                optionsProvider: optionsProvider);

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, "examples.example.neonkube.io.yaml"));

            output.Should().BeEquivalentTo(expectedCrd.TrimEnd());
        }
    }
}
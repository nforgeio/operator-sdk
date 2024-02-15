using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

using FluentAssertions;

using k8s.Models;

using Neon.Common;
using Neon.IO;
using Neon.Operator.Analyzers;
using Neon.Operator.Attributes;
using Neon.Roslyn.Xunit;

using Xunit.Abstractions;

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
            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CustomResourceDefinitionGenerator>()
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .AddSourceFile("Models/ExampleEntity.cs")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            var outFile = "examples.example.neonkube.io.yaml";

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile)).GetHashCodeIgnoringWhitespace();

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile)).GetHashCodeIgnoringWhitespace();
            output.Should().Be(expectedCrd);
        }

        [Fact]
        public void TestGenericCrd()
        {
            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CustomResourceDefinitionGenerator>()
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .AddSourceFile("Models/GenericEntity.cs")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            var outFile = "generics.example.neonkube.io.yaml";

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile)).GetHashCodeIgnoringWhitespace();

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile)).GetHashCodeIgnoringWhitespace();
            output.Should().Be(expectedCrd);
        }

        [Fact]
        public void TestRoslynReflection()
        {
            var source = $@"
using System.Collections.Generic;
using k8s.Models;

namespace TestNamespace
{{
    /// <summary>
    /// The status.
    /// </summary>
    [KubernetesEntity(Group = ""example.neonkube.io"", Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    public class V1Example
    {{
        /// <summary>
        /// Status conditions.
        /// </summary>
        public List<V1Condition> Conditions {{ get; set; }}
    }}
}}";
            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CustomResourceDefinitionGenerator>()
                .AddSource(source)
                .AddAssembly($@"C:\src\operator-sdk\test\Test.Analyzers\bin\Debug\net8.0\KubernetesClient.Models.dll")
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .Build();

            testCompilation.Should().NotBeNull();
        }
    }
}
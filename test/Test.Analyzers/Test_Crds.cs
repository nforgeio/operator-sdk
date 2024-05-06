using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

using FluentAssertions;

using k8s.Models;

using Microsoft.CodeAnalysis;

using Neon.Common;
using Neon.IO;
using Neon.Operator.Analyzers;
using Neon.Operator.Attributes;
using Neon.Roslyn.Xunit;
using Neon.Xunit;

using Xunit.Abstractions;

namespace Test.Analyzers
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
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
                .AddOption("build_property.TargetDir", tempFile.Path)
                .AddSourceFile("Models/V1ExampleEntity.cs")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            var outFile = "examples.example.neonkube.io.g.yaml";

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile));

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile));

            output.GetHashCodeIgnoringWhitespace().Should().Be(expectedCrd.GetHashCodeIgnoringWhitespace());

            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestIgnoreAttributeCrd()
        {
            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CustomResourceDefinitionGenerator>()
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .AddOption("build_property.TargetDir", tempFile.Path)
                .AddSourceFile("Models/V1ExampleEntity.cs")
                .AddSourceFile("Models/V3ExampleEntity.cs")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            var outFile = "examples.example.neonkube.io.g.yaml";

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile));

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile));

            output.GetHashCodeIgnoringWhitespace().Should().Be(expectedCrd.GetHashCodeIgnoringWhitespace());

            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestGenericCrd()
        {
            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CustomResourceDefinitionGenerator>()
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .AddOption("build_property.TargetDir", tempFile.Path)
                .AddSourceFile("Models/GenericEntity.cs")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            var outFile = "generics.example.neonkube.io.g.yaml";

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile));

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile));

            output.GetHashCodeIgnoringWhitespace().Should().Be(expectedCrd.GetHashCodeIgnoringWhitespace());
            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestStorageDiagnostic()
        {
            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CustomResourceDefinitionGenerator>()
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .AddOption("build_property.TargetDir", tempFile.Path)
                .AddSourceFile("Models/V1ExampleEntity.cs")
                .AddSourceFile("Models/V2ExampleEntity.cs")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            testCompilation.Sources.Should().BeEmpty();
            testCompilation.Diagnostics.Should().HaveCount(1);
            testCompilation.Should().HaveDiagnostic(
                diagnostic: Diagnostic.Create(
                    descriptor:  CustomResourceDefinitionGenerator.TooManyStorageVersionsError,
                    location:    Location.None,
                    messageArgs: ["examples.example.neonkube.io", 2]));
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
    [KubernetesEntity(Group = ""example.neonkube.io"", Kind = ""Example"", ApiVersion = ""v1alpha1"", PluralName = ""examples"")]
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
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.TargetDir", tempFile.Path)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .Build();

            testCompilation.Should().NotBeNull();
            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestDictionary()
        {
            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CustomResourceDefinitionGenerator>()
                .AddOption("build_property.NeonOperatorGenerateCrds", true)
                .AddOption("build_property.NeonOperatorCrdOutputDir", tempFile.Path)
                .AddOption("build_property.TargetDir", tempFile.Path)
                .AddSourceFile("Models/ExampleDictEntity.cs")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            var outFile = "dict.example.neonkube.io.g.yaml";

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile));

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile));

            output.GetHashCodeIgnoringWhitespace().Should().Be(expectedCrd.GetHashCodeIgnoringWhitespace());
            testCompilation.Diagnostics.Should().BeEmpty();
        }
    }
}
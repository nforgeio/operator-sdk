using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

using FluentAssertions;

using k8s.Models;

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
            var entityDefinition = File.ReadAllText("Models/ExampleEntity.cs");

            var outFile = "examples.example.neonkube.io.yaml";

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
                    typeof(V1Condition).Assembly,
                    typeof(RequiredAttribute).Assembly,
                ],
                optionsProvider: optionsProvider);

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile));

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile));
            output.Should().BeEquivalentTo(expectedCrd.TrimEnd());
        }

        [Fact]
        public void TestGenericCrd()
        {
            var entityDefinition = File.ReadAllText("Models/GenericEntity.cs");

            var outFile = "generics.example.neonkube.io.yaml";

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

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile));

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile));
            output.Should().BeEquivalentTo(expectedCrd.TrimEnd());
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
// -----------------------------------------------------------------------------
// FILE:	    Test_OLM.cs
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;

using FluentAssertions;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Common;
using Neon.IO;
using Neon.K8s.Core;
using Neon.Operator.Analyzers.Generators;
using Neon.Operator.Attributes;
using Neon.Operator.OperatorLifecycleManager;
using Neon.Roslyn.Xunit;
using Neon.Xunit;

namespace Test.Analyzers
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class Test_Olm
    {
        [Fact]
        public void Test_OlmAttributes()
        {
            var name = "test-operator";
            var displayName = "Testaroo Operator";
            var ownedEntityDesc = $"I'm owned by {name}";
            var requiredEntityDesc = $"Required by {name}";
            var providerName = "NeonSDK";
            var providerUrl = "foo.com";
            var maintainerName = "Bob Testaroni";
            var maintainerEmail = "foo@bar.com";
            var maintainerGitHub = "BobFooGit";
            var version = "1.2.3";
            var maturity = "alpha";
            var minKubeVersion = "1.16.0";
            var containerImage = "github.com/test-operator/cluster-operator";
            var containerImageTag = "1.2.3";
            var repository = "https://github.com/test-operator/cluster-operator";

            var source = $@"
using Neon.Operator.OperatorLifecycleManager;
using Test.Analyzers;
using TestNamespace;
using Neon.Common;

[assembly: Name(""{name}"")]
[assembly: DisplayName(""{displayName}"")]
[assembly: OwnedEntity<V1TestResource>(Description = ""{ownedEntityDesc}"", DisplayName = TestConstants.DisplayName)]
[assembly: RequiredEntity<V1TestResource>(Description = ""{requiredEntityDesc}"", DisplayName = TestConstants.DisplayName)]
[assembly: Description(FullDescription = MoreTestConstants.Description, ShortDescription = ""This is a short description."")]
[assembly: Provider(Name = ""{providerName}"", Url = ""{providerUrl}"")]
[assembly: Maintainer(Name = ""{maintainerName}"", Email = ""{maintainerEmail}"" GitHub = ""{maintainerGitHub}"", Reviewer = true)]
[assembly: Version(""{version}"")]
[assembly: Maturity(""{maturity}"")]
[assembly: MinKubeVersion(""{minKubeVersion}"")]
[assembly: Icon(Path = ""nuget-icon.png"", MediaType = ""image/png"")]
[assembly: Keyword(""foo"", ""bar"", ""baz"")]
[assembly: Category(Category = Category.ApplicationRuntime | Category.DeveloperTools | Category.BigData | Category.BigData)]
[assembly: Capabilities(Capability = CapabilityLevel.DeepInsights)]
[assembly: ContainerImage(Repository = ""{containerImage}"", Tag =""{containerImageTag}"")]
[assembly: Repository(Repository = ""{repository}"")]
[assembly: InstallMode(Type = InstallModeType.OwnNamespace)]
[assembly: InstallMode(Type = InstallModeType.MultiNamespace | InstallModeType.SingleNamespace)]
[assembly: InstallMode(Type = InstallModeType.AllNamespaces, Supported = false)]
[assembly: DefaultChannel(""stable"")]
[assembly: Link(Name = ""GitHub"", Url = ""https://github.com/foo/bar"")]

namespace TestNamespace
{{
    public static class TestConstants
    {{
        public const string DisplayName = ""This is the display name"";
    }}

    public static class MoreTestConstants
    {{
        public const string Description = $@""## This is a heading
Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor
incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis
nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in
culpa qui officia deserunt mollit anim id est laborum."";
    }}
}}
";
            using var temp = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<OlmGenerator>()
                .AddOption("build_property.TargetDir", temp.Path)
                .AddSource(source)
                .AddSourceFile("Models/V1ExampleEntity.cs")
                .AddSourceFile("Models/V2ExampleEntity.cs")
                .AddSourceFile("Controllers/ExampleController.cs")
                .AddSourceFile("Controllers/PodValidator.cs")
                .AddSourceFile("Controllers/PodWebhook.cs")
                .AddAdditionalFilePath("nuget-icon.png")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(NeonHelper).Assembly)
                .AddAssembly(typeof(V1TestResource).Assembly)
                .AddAssembly(typeof(CapabilitiesAttribute).Assembly)
                .AddAssembly(typeof(ResourceControllerAttribute).Assembly)
                .AddAssembly(typeof(DefaultValueAttribute).Assembly)
                .Build();

            testCompilation.Diagnostics.Should().BeEmpty();

            var outFile = Path.Combine(temp.Path, "OperatorLifecycleManager", version,  "manifests", $"{name}.clusterserviceversion.yaml");

            File.Exists(outFile).Should().BeTrue();

            var output = File.ReadAllText(outFile);

            var outCsv = KubernetesHelper.YamlDeserialize<V1ClusterServiceVersion>(output);

            outCsv.Metadata.Name.Should().Be($"{name}.v{version}");

            // check annotations
            outCsv.Metadata.Annotations["categories"].Should().Be($"{Category.ApplicationRuntime.ToMemberString()}, {Category.BigData.ToMemberString()}, {Category.DeveloperTools.ToMemberString()}");
            outCsv.Metadata.Annotations["certified"].Should().Be("false");
            outCsv.Metadata.Annotations["capabilities"].Should().Be(CapabilityLevel.DeepInsights.ToMemberString());
            outCsv.Metadata.Annotations["containerImage"].Should().Be($"{containerImage}:{containerImageTag}");
            outCsv.Metadata.Annotations["repository"].Should().Be(repository);
            outCsv.Metadata.Annotations["alm-examples"].Should().Be($@"[{{""spec"":{{""message"":""default message""}},""apiVersion"":""test.neonkube.io/v1alpha1"",""kind"":""NeonTestObject"",""metadata"":{{""name"":""my-neontestobject"",""namespace"":""default""}}}}]");

            // check spec
            outCsv.Spec.DisplayName.Should().Be(displayName);
            outCsv.Spec.CustomResourceDefinitions.Owned.Should().HaveCount(1);
            outCsv.Spec.CustomResourceDefinitions.Owned.First().Description.Should().Be(ownedEntityDesc);
            outCsv.Spec.CustomResourceDefinitions.Required.Should().HaveCount(1);
            outCsv.Spec.CustomResourceDefinitions.Required.First().Description.Should().Be(requiredEntityDesc);
            outCsv.Spec.Provider.Name.Should().Be(providerName);
            outCsv.Spec.Provider.Url.Should().Be(providerUrl);
            outCsv.Spec.Maintainers.Should().HaveCount(1);
            outCsv.Spec.Maintainers.First().Name.Should().Be(maintainerName);
            outCsv.Spec.Maintainers.First().Email.Should().Be(maintainerEmail);
            outCsv.Spec.Version.Should().Be(version);
            outCsv.Spec.Maturity.Should().Be(maturity);
            outCsv.Spec.MinKubeVersion.Should().Be(minKubeVersion);
            outCsv.Spec.Keywords.Should().Contain("foo");
            outCsv.Spec.Keywords.Should().Contain("bar");
            outCsv.Spec.Keywords.Should().Contain("baz");
            outCsv.Spec.Install.Spec.Deployments.First().Spec.Template.Spec.Containers.First().Image.Should().Be($"{containerImage}:{containerImageTag}");

            // check webhooks
            outCsv.Spec.WebHookDefinitions.Should().HaveCount(2);
            outCsv.Spec.WebHookDefinitions.Where(wd => wd.Type == WebhookAdmissionType.ValidatingAdmissionWebhook).Should().HaveCount(1);
            outCsv.Spec.WebHookDefinitions.Where(wd => wd.Type == WebhookAdmissionType.MutatingAdmissionWebhook).Should().HaveCount(1);
        }


        [Fact]
        public void TestMinimal()
        {
            var source = $@"
using Neon.Operator.OperatorLifecycleManager;
using Test.Analyzers;
using TestNamespace;
using Neon.Common;

[assembly: Name(""my-operator"")]
[assembly: DisplayName(""My Operator"")]
[assembly: Version(""1.2.3"")]
[assembly: Description(
    ShortDescription = ""short description"",
    FullDescription = ""This is the full description."")]
[assembly: Keyword(""my"", ""example"", ""operator"")]
[assembly: Category(
    Category = Category.DeveloperTools | Category.ApplicationRuntime)]
[assembly: Capabilities(
    Capability = CapabilityLevel.DeepInsights)]
[assembly: ContainerImage(
    Repository = ""github.com/test-operator/cluster-operator"",
    Tag =""1.2.3"")]
[assembly: InstallMode(
    Type = InstallModeType.OwnNamespace | InstallModeType.MultiNamespace,
    Supported = true)]
[assembly: Provider(Name = ""Example"")]
[assembly: Maintainer(
    Name = ""Some Corp"",
    Email = ""foo@bar.com"")]
[assembly: Icon(Path = ""nuget-icon.png"", MediaType = ""image/png"")]
[assembly: DefaultChannel(""stable"")]
[assembly: Link(Name = ""GitHub"", Url = ""https://github.com/foo/bar"")]
";
            using var temp = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<OlmGenerator>()
                .AddOption("build_property.TargetDir", temp.Path)
                .AddSource(source)
                .AddAdditionalFilePath("nuget-icon.png")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(NeonHelper).Assembly)
                .AddAssembly(typeof(V1TestResource).Assembly)
                .AddAssembly(typeof(CapabilitiesAttribute).Assembly)
                .AddAssembly(typeof(ResourceControllerAttribute).Assembly)
                .AddAssembly(typeof(DefaultValueAttribute).Assembly)
                .Build();

            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestCategoryFlags()
        {
            var category = Category.Database | Category.BigData;

            category.ToStrings().Should().BeEquivalentTo(new[] { Category.Database.ToMemberString(), Category.BigData.ToMemberString() });
        }

        [Fact]
        public void TestCategoryFlagsWithDuplicate()
        {
            var categories = new List<Category>()
            {
                Category.Database | Category.BigData,
                Category.BigData
            };

            var result = string.Join(", ", categories.SelectMany(c => c.ToStrings()).ToImmutableHashSet().Order());
            result.Should().Be($"{Category.BigData.ToMemberString()}, {Category.Database.ToMemberString()}");
        }

        [Fact]
        public void TestReviewerDiagnostic()
        {
            var maintainerName = "bob testaroni";

            var source = $@"
using Neon.Operator.OperatorLifecycleManager;

[assembly: Maintainer(Name = ""eric testaroo"", Email = ""foo@bar.com"", Reviewer = false)]
[assembly: Maintainer(Name = ""{maintainerName}"", Email = ""bar@baz.com"", Reviewer = true)]
";

            using var temp = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<OlmGenerator>()
                .AddOption("build_property.TargetDir", temp.Path)
                .AddSource(source)
                .AddAssembly(typeof(MaintainerAttribute).Assembly)
                .Build();

            var syntax = testCompilation
                .Compilation
                .SyntaxTrees
                .First()
                .GetRoot()
                .DescendantNodes()
                .OfType<AttributeSyntax>()
                .Last();

            testCompilation.Should().HaveDiagnostic(Diagnostic.Create(
                OlmGenerator.MissingReviewerError,
                syntax.GetLocation(),
                [maintainerName]));
        }

        [Fact]
        public void TestInstallModeDiagnostic()
        {
            var maintainerName = "bob testaroni";

            var source = $@"
using Neon.Operator.OperatorLifecycleManager;

[assembly: InstallMode(Type = InstallModeType.OwnNamespace, Supported = true)]
[assembly: InstallMode(Type = InstallModeType.OwnNamespace, Supported = false)]
";
            using var temp = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<OlmGenerator>()
                .AddOption("build_property.TargetDir", temp.Path)
                .AddSource(source)
                .AddAssembly(typeof(InstallModeAttribute).Assembly)
                .Build();

            var syntax = testCompilation
                .Compilation
                .SyntaxTrees
                .First()
                .GetRoot()
                .DescendantNodes()
                .OfType<AttributeSyntax>()
                .Last();

            testCompilation.Should().HaveDiagnostic(Diagnostic.Create(
                OlmGenerator.InstallModeDuplicateError,
                syntax.GetLocation(),
                [maintainerName]));
        }

        [Fact]
        public void TestExampleJson()
        {
            var example = new V1TestResource().Initialize();
            example.Metadata.Name = "test";
            example.Metadata.NamespaceProperty = "test";
            example.Spec = new TestSpec();
            example.Spec.Message = "test message";

            var exampleJson = KubernetesHelper.JsonSerialize(example);
            var escapedExampleJson = exampleJson.Replace("\"", "\\\"");
            var source = $@"
using Neon.Operator.OperatorLifecycleManager;
using Test.Analyzers;

[assembly: OwnedEntity<V1TestResource>(Description = ""This is a description"", DisplayName = ""Name"", ExampleJson = ""{escapedExampleJson}"")]
";
            using var temp = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<OlmGenerator>()
                .AddOption("build_property.TargetDir", temp.Path)
                .AddSource(source)
                .AddAssembly(typeof(V1TestResource).Assembly)
                .AddAssembly(typeof(OwnedEntityAttribute).Assembly)
                .Build();

            var outFile = Path.Combine(temp.Path, "OperatorLifecycleManager", "manifests", $".clusterserviceversion.yaml");

            File.Exists(outFile).Should().BeTrue();

            var output = File.ReadAllText(outFile);

            var outCsv = KubernetesHelper.YamlDeserialize<V1ClusterServiceVersion>(output);

            outCsv.Metadata.Annotations["alm-examples"].Should().Be($"[{exampleJson}]");
        }

        [Fact]
        public void TestExampleYaml()
        {
            var example = new V1TestResource().Initialize();
            example.Metadata.Name = "test";
            example.Metadata.NamespaceProperty = "test";
            example.Spec = new TestSpec();
            example.Spec.Message = "test message";

            var exampleJson = KubernetesHelper.JsonSerialize(example);

            var source = $@"
using Neon.Operator.OperatorLifecycleManager;
using Test.Analyzers;
using TestNamespace;

[assembly: OwnedEntity<V1TestResource>(Description = ""This is a description"", DisplayName = ""Name"", ExampleYaml = TestConstants.ExampleYaml)]
";
            var constants = $@"
namespace TestNamespace
{{
    public class TestConstants
    {{
        public const string ExampleYaml = $@""apiVersion: test.neonkube.io/v1alpha1
kind: NeonTestObject
metadata:
  name: test
  namespace: test
spec:
  message: test message"";
    }}
}}";

            using var temp = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<OlmGenerator>()
                .AddOption("build_property.TargetDir", temp.Path)
                .AddSource(source)
                .AddSource(constants)
                .AddAssembly(typeof(V1TestResource).Assembly)
                .AddAssembly(typeof(OwnedEntityAttribute).Assembly)
                .Build();

            var outFile = Path.Combine(temp.Path, "OperatorLifecycleManager", "manifests", $".clusterserviceversion.yaml");

            File.Exists(outFile).Should().BeTrue();

            var output = File.ReadAllText(outFile);

            var outCsv = KubernetesHelper.YamlDeserialize<V1ClusterServiceVersion>(output);

            outCsv.Metadata.Annotations["alm-examples"].Should().Be($"[{exampleJson}]");
        }

        [Fact]
        public void TestExampleJsonYamlDiagnostic()
        {

            var example = new V1TestResource().Initialize();
            example.Spec = new TestSpec();
            example.Spec.Message = "test message";

            var exampleJson = KubernetesHelper.JsonSerialize(example).Replace("\"", "\\\"");

            var source = $@"
using Neon.Operator.OperatorLifecycleManager;
using Test.Analyzers;
using TestNamespace;

[assembly: OwnedEntity<V1TestResource>(Description = ""This is a description"", DisplayName = ""Name"", ExampleJson = ""{exampleJson}"", ExampleYaml = TestConstants.ExampleYaml)]
";
            var constants = $@"
namespace TestNamespace
{{
    public class TestConstants
    {{
        public const string ExampleYaml = $@""apiVersion: test.neonkube.io/v1alpha1
kind: V1TestResource
metadata:
  name: test
  namespace: test
spec:
  message: test message"";
    }}
}}";

            using var temp = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<OlmGenerator>()
                .AddOption("build_property.TargetDir", temp.Path)
                .AddSource(source)
                .AddSource(constants)
                .AddAssembly(typeof(V1TestResource).Assembly)
                .AddAssembly(typeof(OwnedEntityAttribute).Assembly)
                .Build();

            var syntax = testCompilation
                .Compilation
                .SyntaxTrees
                .First()
                .GetRoot()
                .DescendantNodes()
                .OfType<AttributeSyntax>()
                .Last();

            testCompilation.Should().HaveDiagnostic(Diagnostic.Create(
                OlmGenerator.OwnedAttributeExampleError,
                syntax.GetLocation(),
                ["V1TestResource"]));
        }
    }

}

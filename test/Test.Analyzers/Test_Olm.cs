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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

using FluentAssertions;

using k8s.Models;

using Neon.Common;
using Neon.IO;
using Neon.Operator.Analyzers;
using Neon.Operator.Analyzers.Generators;
using Neon.Operator.Attributes;
using Neon.Operator.OperatorLifecycleManager;
using Neon.Roslyn.Xunit;

namespace Test.Analyzers
{
    public class Test_Olm
    {
        [Fact]
        public void Test_OlmAttributes()
        {
            var source = $@"
using Neon.Operator.OperatorLifecycleManager;
using Test.Analyzers;
using TestNamespace;
using Neon.Common;

[assembly: Name(""test-operator"")]
[assembly: DisplayName(""testaroo operator"")]
[assembly: OwnedEntity<V1TestResource>(Description = ""This is the description"", DisplayName = TestConstants.DisplayName)]
[assembly: RequiredEntity<V1TestResource>(Description = ""This is the required description"", DisplayName = TestConstants.DisplayName)]
[assembly: Description(FullDescription = MoreTestConstants.Description, ShortDescription = ""This is a short description."")]
[assembly: Provider(Name = ""Example"", Url = ""www.example.com"")]
[assembly: Maintainer(Name = NeonHelper.NeonMetricsPrefix, Email = ""foo@bar.com"")]
[assembly: Version(""1.2.3"")]
[assembly: Maturity(""alpha"")]
[assembly: MinKubeVersion(""1.16.0"")]
[assembly: Icon(Path = ""nuget-icon.png"", MediaType = ""image/png"")]
[assembly: Keyword(""foo"", ""bar"", ""baz"")]
[assembly: Type(Supported = true, Type = InstallModeType.OwnNamespace)]
[assembly: Category(Category = Category.ApplicationRuntime | Category.DeveloperTools | Category.BigData | Category.BigData)]
[assembly: Capabilities(Capability = CapabilityLevel.DeepInsights)]
[assembly: ContainerImage(Repository = ""github.com/test-operator/cluster-operator"", Tag =""1.2.3"")]
[assembly: Repository(Repository = ""https://github.com/test-operator/cluster-operator"")]
[assembly: InstallMode(Type = InstallModeType.OwnNamespace)]
[assembly: InstallMode(Type = InstallModeType.MultiNamespace | InstallModeType.SingleNamespace)]
[assembly: InstallMode(Type = InstallModeType.AllNamespaces, Supported = false)]


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
                .AddAdditionalFilePath("nuget-icon.png")
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(NeonHelper).Assembly)
                .AddAssembly(typeof(V1TestResource).Assembly)
                .AddAssembly(typeof(CapabilitiesAttribute).Assembly)
                .AddAssembly(typeof(ResourceControllerAttribute).Assembly)
                .Build();

            var outFile = Path.Combine(temp.Path, "clusterserviceversion.yaml");

            File.Exists(outFile).Should().BeTrue();

            var output = File.ReadAllText(outFile);
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

            var result = string.Join(", ", categories.SelectMany(c => c.ToStrings()).ToImmutableHashSet());
            result.Should().Be($"{Category.BigData.ToMemberString()}, {Category.Database.ToMemberString()}");
        }
    }
}

// -----------------------------------------------------------------------------
// FILE:	    Test_Rbac.cs
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

using System.ComponentModel.DataAnnotations;
using System.IO;

using FluentAssertions;

using k8s.Models;

using Neon.Common;
using Neon.IO;
using Neon.Operator.Analyzers;
using Neon.Operator.Attributes;
using Neon.Roslyn.Xunit;
using Neon.Xunit;

namespace Test.Analyzers
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class Test_Rbac
    {

        [Fact]
        public void TestRbacRuleOnProgram()
        {
            var classDefinition = $@"using System.Threading.Tasks;

using k8s.Models;

using Microsoft.AspNetCore.Hosting;

using Neon.Operator;
using Neon.Operator.Attributes;
using Neon.Operator.Rbac;

namespace TestOperator.Foo.Bar
{{
    [RbacRule<V1ConfigMap>(Verbs = RbacVerb.All, Scope = EntityScope.Cluster)]
    [RbacRule<V1Secret>(Verbs = RbacVerb.All, Scope = EntityScope.Cluster)]
    [RbacRule<V1Service>(Verbs = Neon.Operator.Rbac.RbacVerb.Watch)]
    [RbacRule<V1Pod>(Verbs = Neon.Operator.Rbac.RbacVerb.Watch)]
    public static class Program
    {{
        public static async Task Main(string[] args)
        {{
            var k8s = KubernetesOperatorHost
               .CreateDefaultBuilder()
               .ConfigureOperator(configure =>
               {{
                   configure.AssemblyScanningEnabled = false;
                   configure.PodNamespace            = ""default"";
               }})
               .ConfigureNeonKube()
               .UseStartup<Startup>().Build();

            await k8s.RunAsync();
        }}
    }}
}}";


            using var tempFile = new TempFolder();

            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<RbacRuleGenerator>()
                .AddOption("build_property.NeonOperatorName", "test-operator")
                .AddOption("build_property.NeonOperatorGenerateRbac", true)
                .AddOption("build_property.NeonOperatorRbacOutputDir", tempFile.Path)
                .AddOption("build_property.TargetDir", tempFile.Path)
                .AddSource(classDefinition)
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .AddAssembly(typeof(V1Condition).Assembly)
                .AddAssembly(typeof(RequiredAttribute).Assembly)
                .Build();

            var outFile = "role-test-operator.yaml";

            var output =  File.ReadAllText(Path.Combine(tempFile.Path, outFile)).GetHashCodeIgnoringWhitespace();

            var expectedCrd = File.ReadAllText(Path.Combine("Outputs", outFile)).GetHashCodeIgnoringWhitespace();
            output.Should().Be(expectedCrd);
        }
    }
}

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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

using FluentAssertions;

using k8s.Models;

using Neon.IO;
using Neon.Operator.Analyzers;
using Neon.Operator.Attributes;

namespace Test.Analyzers
{
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
                   configure.DeployedNamespace       = ""default"";
               }})
               .ConfigureNeonKube()
               .UseStartup<Startup>().Build();

            await k8s.RunAsync();
        }}
    }}
}}";

            var outFile = "role-test-operator.yaml";

            using var tempFile = new TempFolder();

            var optionsProvider = new OperatorAnalyzerConfigOptionsProvider();
            optionsProvider.SetOptions(new OperatorAnalyzerConfigOptions()
            {
                Options = new Dictionary<string, string>()
                {
                    {"build_property.NeonOperatorName", "test-operator" },
                    {"build_property.NeonOperatorGenerateCrds", "true" },
                    {"build_property.NeonOperatorRbacOutputDir", tempFile.Path }
                }
            });

            var generatedCode = CompilationHelper.GetGeneratedOutput<RbacRuleGenerator>(
                source: classDefinition,
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
    }
}

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
using System.IO;

using FluentAssertions;

using k8s.Models;

using Neon.IO;
using Neon.Operator.Analyzers.Generators;

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
[assembly: OwnedEntity<V1TestResource>]";
            using var temp = new TempFolder();

            var optionsProvider = new OperatorAnalyzerConfigOptionsProvider();
            optionsProvider.SetOptions(new OperatorAnalyzerConfigOptions()
            {
                Options = new Dictionary<string, string>()
                {
                    {"build_property.TargetDir", temp.Path },
                }
            });

            var generatedCode = CompilationHelper.GetGeneratedOutput<OlmGenerator>(
                source: source,
                additionalAssemblies: [
                    typeof(KubernetesEntityAttribute).Assembly,
                    typeof(V1TestResource).Assembly,
                ],
                optionsProvider: optionsProvider);

            var outFile = Path.Combine(temp.Path, "clusterserviceversion.yaml");

            File.Exists(outFile).Should().BeTrue();
        }
    }
}

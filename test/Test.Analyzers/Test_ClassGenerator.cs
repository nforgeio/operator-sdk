// -----------------------------------------------------------------------------
// FILE:	    Test_ClassGenerator.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using k8s.Models;
using Neon.IO;

using Neon.Operator.Analyzers;
using Neon.Operator.Analyzers.Generators;
using Neon.Operator.Attributes;
using Neon.Operator.Webhooks;
using Neon.Roslyn.Xunit;
using Neon.K8s.Resources;
using k8s;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Test.Analyzers
{
    public class Test_ClassGenerator
    {
        [Fact]
        public void TestCrontabExample()
        {
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CrdClassGenerator>()
                .AddAdditionalFilePath("CRDs/crontab.yaml")
                .Build();

            var syntaxStrings = testCompilation.Compilation.SyntaxTrees.Select(t => t.ToString()).ToList();

            testCompilation.HasOutputSyntax($@"using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neon.Operator.Attributes;
using Neon.Operator.Resources;
using k8s;
using k8s.Models;

namespace Neon.Operator.Resources
{{
    [KubernetesEntityAttribute(Group = ""stable.example.com"", Kind = ""CronTab"", ApiVersion = ""v1"", PluralName = ""crontabs"")]
    public class V1CronTab : IKubernetesObject<V1ObjectMeta>, ISpec<global::Neon.Operator.Resources.V1CronTabSpec>
    {{
        public V1CronTab()
        {{
            ApiVersion = ""stable.example.com/v1"";
            Kind = ""CronTab"";
        }}

        public string ApiVersion {{ get; set; }}
        public string Kind {{ get; set; }}
        public V1ObjectMeta Metadata {{ get; set; }}
        public global::Neon.Operator.Resources.V1CronTabSpec Spec {{ get; set; }}
    }}
}}").Should().BeTrue();

            testCompilation.HasOutputSyntax($@"using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neon.Operator.Attributes;
using Neon.Operator.Resources;
using k8s;
using k8s.Models;

namespace Neon.Operator.Resources
{{
    public class V1CronTabSpec
    {{
        public string CronSpec {{ get; set; }}
        public string Image {{ get; set; }}
        public long? Replicas {{ get; set; }}
    }}
}}").Should().BeTrue();

            testCompilation.Compilation.SyntaxTrees.Should().HaveCount(3);

        }

        [Fact]
        public void TestV1ServiceMonitor()
        {
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CrdClassGenerator>()
                .AddAdditionalFilePath("CRDs/servicemonitor.yaml")
                .Build();

            var syntaxStrings = testCompilation.Compilation.SyntaxTrees.Select(t => t.ToString()).ToList();
        }

        [Fact]
        public void TestV1ServiceMonitorEquals()
        {
            var sm0 = new Neon.K8s.Resources.Prometheus.V1ServiceMonitor().Initialize();
            sm0.Spec = new Neon.K8s.Resources.Prometheus.V1ServiceMonitorSpec()
            {
                Endpoints = [
                    new Neon.K8s.Resources.Prometheus.Endpoint(){
                        Interval = "1m",
                        HonorLabels = true,
                        TargetPort = 999
                    }
                ],
                JobLabel = "job-label",
                LabelLimit = 10,
                LabelNameLengthLimit = 10,
                SampleLimit = 10,
                Selector = new V1LabelSelector()
                {
                    MatchExpressions = [
                        new V1LabelSelectorRequirement(){
                            Key = "foo",
                            OperatorProperty = "bar",
                        }
                    ]
                },
            };

            var sm1 = new Neon.Operator.Resources.V1ServiceMonitor().Initialize();
            sm1.Spec = new Neon.Operator.Resources.V1ServiceMonitorSpec()
            {
                Endpoints = [
                    new Neon.Operator.Resources.Endpoint(){
                        Interval = "1m",
                        HonorLabels = true,
                        TargetPort = 999
                    }
                ],
                JobLabel = "job-label",
                LabelLimit = 10,
                LabelNameLengthLimit = 10,
                SampleLimit = 10,
                Selector = new Neon.Operator.Resources.Selector()
                {
                    MatchExpressions = [
                        new Neon.Operator.Resources.MatchExpression() {
                            Key = "foo",
                            Operator = "bar"
                        }
                    ]
                }
            };

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { JsonExtensions.AlphabetizeProperties() },
                },
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var sm0String = KubernetesJson.Serialize(sm0, options);
            var sm1String = KubernetesJson.Serialize(sm1, options);

            sm0String.Should().Be(sm1String);
        }
    }


    public static partial class JsonExtensions
    {
        public static Action<JsonTypeInfo> AlphabetizeProperties(Type type)
        {
            return typeInfo =>
            {
                if (typeInfo.Kind != JsonTypeInfoKind.Object || !type.IsAssignableFrom(typeInfo.Type))
                    return;
                AlphabetizeProperties()(typeInfo);
            };
        }

        public static Action<JsonTypeInfo> AlphabetizeProperties()
        {
            return static typeInfo =>
            {
                if (typeInfo.Kind != JsonTypeInfoKind.Object)
                    return;
                var properties = typeInfo.Properties.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
                typeInfo.Properties.Clear();
                for (int i = 0; i < properties.Count; i++)
                {
                    properties[i].Order = i;
                    typeInfo.Properties.Add(properties[i]);
                }
            };
        }
    }
}

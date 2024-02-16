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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using FluentAssertions;

using k8s;
using k8s.Models;

using Neon.Operator.Analyzers;
using Neon.Operator.Webhooks;
using Neon.Roslyn.Xunit;

namespace Test.Analyzers
{
    [Collection("Analyzers")]
    public class Test_ClassGenerator
    {
        [Fact]
        public void TestCrontabExample()
        {
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CrdClassGenerator>()
                .AddAdditionalFilePath("CRDs/crontab.yaml")
                .Build();

            testCompilation.Should().ContainSource($@"namespace Neon.Operator.Resources
{{
    [global::k8s.Models.KubernetesEntityAttribute(Group = ""stable.example.com"", Kind = ""CronTab"", ApiVersion = ""v1"", PluralName = ""crontabs"")]
    public partial class V1CronTab : global::k8s.IKubernetesObject<global::k8s.Models.V1ObjectMeta>, global::k8s.ISpec<global::Neon.Operator.Resources.V1CronTab.V1CronTabSpec>
    {{
        public V1CronTab()
        {{
            ApiVersion = ""stable.example.com/v1"";
            Kind = ""CronTab"";
        }}

        public global::System.String ApiVersion {{ get; set; }}
        public global::System.String Kind {{ get; set; }}
        public global::k8s.Models.V1ObjectMeta Metadata {{ get; set; }}
        public global::Neon.Operator.Resources.V1CronTab.V1CronTabSpec Spec {{ get; set; }}
    }}
}}");

            testCompilation.Should().ContainSource($@"namespace Neon.Operator.Resources
{{
    public partial class V1CronTab
    {{
        public class V1CronTabSpec
        {{
            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""cronSpec"")]
            public string CronSpec {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""image"")]
            public string Image {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""replicas"")]
            public global::System.Int64? Replicas {{ get; set; }}
        }}
    }}
}}");

            testCompilation.Sources.Should().HaveCount(2);
            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestChangeNamespace()
        {
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CrdClassGenerator>()
                .AddOption("build_property.CrdTargetNamespace", "Test.Namespace")
                .AddAdditionalFilePath("CRDs/crontab.yaml")
                .Build();

            testCompilation.Should().ContainSource($@"namespace Test.Namespace
{{
    [global::k8s.Models.KubernetesEntityAttribute(Group = ""stable.example.com"", Kind = ""CronTab"", ApiVersion = ""v1"", PluralName = ""crontabs"")]
    public partial class V1CronTab : global::k8s.IKubernetesObject<global::k8s.Models.V1ObjectMeta>, global::k8s.ISpec<global::Test.Namespace.V1CronTab.V1CronTabSpec>
    {{
        public V1CronTab()
        {{
            ApiVersion = ""stable.example.com/v1"";
            Kind = ""CronTab"";
        }}

        public global::System.String ApiVersion {{ get; set; }}
        public global::System.String Kind {{ get; set; }}
        public global::k8s.Models.V1ObjectMeta Metadata {{ get; set; }}
        public global::Test.Namespace.V1CronTab.V1CronTabSpec Spec {{ get; set; }}
    }}
}}");

            testCompilation.Should().ContainSource($@"namespace Test.Namespace
{{
    public partial class V1CronTab
    {{
        public class V1CronTabSpec
        {{
            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""cronSpec"")]
            public string CronSpec {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""image"")]
            public string Image {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""replicas"")]
            public global::System.Int64? Replicas {{ get; set; }}
        }}
    }}
}}");

            testCompilation.Sources.Should().HaveCount(2);
            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestV1ServiceMonitor()
        {
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CrdClassGenerator>()
                .AddAdditionalFilePath("CRDs/servicemonitor.yaml")
                .Build();

            testCompilation.Should().ContainSource($@"namespace Neon.Operator.Resources
{{
    [global::k8s.Models.KubernetesEntityAttribute(Group = ""monitoring.coreos.com"", Kind = ""ServiceMonitor"", ApiVersion = ""v1"", PluralName = ""servicemonitors"")]
    public partial class V1ServiceMonitor : global::k8s.IKubernetesObject<global::k8s.Models.V1ObjectMeta>, global::k8s.ISpec<global::Neon.Operator.Resources.V1ServiceMonitor.V1ServiceMonitorSpec>
    {{
        public V1ServiceMonitor()
        {{
            ApiVersion = ""monitoring.coreos.com/v1"";
            Kind = ""ServiceMonitor"";
        }}

        public global::System.String ApiVersion {{ get; set; }}
        public global::System.String Kind {{ get; set; }}
        public global::k8s.Models.V1ObjectMeta Metadata {{ get; set; }}
        public global::Neon.Operator.Resources.V1ServiceMonitor.V1ServiceMonitorSpec Spec {{ get; set; }}
    }}
}}");

            testCompilation.Should().ContainSource($@"namespace Neon.Operator.Resources
{{
    public partial class V1ServiceMonitor
    {{
        public class V1ServiceMonitorSpec
        {{
            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""attachMetadata"")]
            public global::Neon.Operator.Resources.V1ServiceMonitor.AttachMetadata AttachMetadata {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""endpoints"")]
            public global::System.Collections.Generic.List<global::Neon.Operator.Resources.V1ServiceMonitor.Endpoint> Endpoints {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""jobLabel"")]
            public string JobLabel {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""keepDroppedTargets"")]
            public global::System.Int64? KeepDroppedTargets {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""labelLimit"")]
            public global::System.Int64? LabelLimit {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""labelNameLengthLimit"")]
            public global::System.Int64? LabelNameLengthLimit {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""labelValueLengthLimit"")]
            public global::System.Int64? LabelValueLengthLimit {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""namespaceSelector"")]
            public global::Neon.Operator.Resources.V1ServiceMonitor.NamespaceSelector NamespaceSelector {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""podTargetLabels"")]
            public global::System.Collections.Generic.List<global::System.String> PodTargetLabels {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""sampleLimit"")]
            public global::System.Int64? SampleLimit {{ get; set; }}

            [global::System.ComponentModel.DataAnnotations.RequiredAttribute]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""selector"")]
            public global::Neon.Operator.Resources.V1ServiceMonitor.Selector Selector {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""targetLabels"")]
            public global::System.Collections.Generic.List<global::System.String> TargetLabels {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""targetLimit"")]
            public global::System.Int64? TargetLimit {{ get; set; }}
        }}
    }}
}}");

            testCompilation.Should().ContainSource($@"namespace Neon.Operator.Resources
{{
    public partial class V1ServiceMonitor
    {{
        [global::System.Text.Json.Serialization.JsonConverterAttribute(typeof(global::System.Text.Json.Serialization.JsonStringEnumMemberConverter))]
        public enum ActionType
        {{
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""Replace"")]
            Replace,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""Keep"")]
            Keep,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""Drop"")]
            Drop,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""HashMod"")]
            HashMod,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""LabelMap"")]
            LabelMap,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""LabelDrop"")]
            LabelDrop,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""LabelKeep"")]
            LabelKeep,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""Lowercase"")]
            Lowercase,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""Uppercase"")]
            Uppercase,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""KeepEqual"")]
            KeepEqual,
            [global::System.Runtime.Serialization.EnumMemberAttribute(Value = ""DropEqual"")]
            DropEqual
        }}
    }}
}}");

            testCompilation.Should().ContainSource($@"namespace Neon.Operator.Resources
{{
    public partial class V1ServiceMonitor
    {{
        public class MetricRelabeling
        {{
            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""action"")]
            public ActionType Action {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""modulus"")]
            public global::System.Int64? Modulus {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""regex"")]
            public string Regex {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""replacement"")]
            public string Replacement {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""separator"")]
            public string Separator {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""sourceLabels"")]
            public global::System.Collections.Generic.List<global::System.String> SourceLabels {{ get; set; }}

            [global::System.ComponentModel.DefaultValueAttribute(null)]
            [global::System.Text.Json.Serialization.JsonPropertyNameAttribute(""targetLabel"")]
            public string TargetLabel {{ get; set; }}
        }}
    }}
}}");

            testCompilation.Sources.Should().HaveCount(26);
            testCompilation.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void TestV1ServiceMonitorEquals()
        {
            // create servicemonitor using existing k8s client classes.
            var sm0 = new Neon.K8s.Resources.Prometheus.V1ServiceMonitor().Initialize();
            sm0.Spec = new Neon.K8s.Resources.Prometheus.V1ServiceMonitorSpec()
            {
                Endpoints = [
                    new Neon.K8s.Resources.Prometheus.Endpoint(){
                        Interval = "1m",
                        HonorLabels = true,
                        TargetPort = 999,
                        Path = "/metrics",
                        Port = "http-metrics",
                        Scheme = "https"
                    }
                ],
                JobLabel = "job-label",
                LabelLimit = 10,
                LabelNameLengthLimit = 10,
                SampleLimit = 10,
                LabelValueLengthLimit = 10,
                NamespaceSelector = new Neon.K8s.Resources.Prometheus.NamespaceSelector()
                {
                    MatchNames = [ "foo", "bar" ]
                },
                TargetLabels = ["foo", "123"],
                PodTargetLabels = ["foo"],
                TargetLimit = null,
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

            // create servicemonitor with same values using roslyn generated classes.
            var sm1 = new Neon.Operator.Resources.V1ServiceMonitor().Initialize();
            sm1.Spec = new Neon.Operator.Resources.V1ServiceMonitor.V1ServiceMonitorSpec()
            {
                Endpoints = [
                    new Neon.Operator.Resources.V1ServiceMonitor.Endpoint(){
                        Interval = "1m",
                        HonorLabels = true,
                        TargetPort = 999,
                        Path = "/metrics",
                        Port = "http-metrics",
                        Scheme = Neon.Operator.Resources.V1ServiceMonitor.SchemeType.Https
                    }
                ],
                JobLabel = "job-label",
                LabelLimit = 10,
                LabelNameLengthLimit = 10,
                SampleLimit = 10,
                LabelValueLengthLimit = 10,
                NamespaceSelector = new Neon.Operator.Resources.V1ServiceMonitor.NamespaceSelector()
                {
                    MatchNames = ["foo", "bar"],
                    Any = false
                },
                TargetLabels = ["foo", "123"],
                PodTargetLabels = ["foo"],
                TargetLimit = null,
                Selector = new Neon.Operator.Resources.V1ServiceMonitor.Selector()
                {
                    MatchExpressions = [
                        new Neon.Operator.Resources.V1ServiceMonitor.MatchExpression() {
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
                WriteIndented          = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            };

            var sm0String = KubernetesJson.Serialize(sm0, options);
            var sm1String = KubernetesJson.Serialize(sm1, options);

            sm0String.Should().Be(sm1String);
        }

        [Fact]
        public void TestCsv()
        {
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<CrdClassGenerator>()
                .AddAdditionalFilePath("CRDs/csv.yaml")
                .Build();

            var syntaxStrings = testCompilation.Compilation.SyntaxTrees.Select(t => t.ToString()).ToList();

            testCompilation.Sources.Should().HaveCount(33);
            testCompilation.Diagnostics.Should().BeEmpty();
        }
    }


    /// <summary>
    /// Used for testing, to ensure that serialization is consistent.
    /// </summary>
    internal static partial class JsonExtensions
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

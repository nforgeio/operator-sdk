// -----------------------------------------------------------------------------
// FILE:	    TestCoreResources.cs
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
using System.Threading.Tasks;

using FluentAssertions;

using k8s;
using k8s.Models;

using Neon.K8s.Core;
using Neon.Operator.Util;
using Neon.Operator.Xunit;
using Neon.Xunit;

using Test.Neon.Operator;

using Xunit;

namespace TestKubeOperator
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class TestCoreResources : IClassFixture<OperatorFixture>
    {
        private OperatorFixture fixture;

        public TestCoreResources(OperatorFixture fixture)
        {
            this.fixture = fixture;
            fixture.RegisterType<V1ConfigMap>();
            fixture.RegisterType<V1Service>();
            fixture.RegisterType<V1StatefulSet>();
            fixture.RegisterType<V1Job>();
            fixture.Start();
        }

        [Fact]
        public async Task TestGetConfigMapAsync()
        {
            fixture.ClearResources();

            var configMap = new V1ConfigMap()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = "test",
                    NamespaceProperty = "test",
                },
                Data = new Dictionary<string, string>(){ { "foo", "bar" } }
            };

            fixture.AddResource<V1ConfigMap>(configMap);

            var result = await fixture.KubernetesClient.CoreV1.ReadNamespacedConfigMapAsync(configMap.Metadata.Name, configMap.Metadata.NamespaceProperty);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task TestGetServiceAsync()
        {
            fixture.ClearResources();

            var service = new V1Service().Initialize();
            service.Metadata.Name = "test";
            service.Metadata.NamespaceProperty = "test-ns";
            service.Spec = new V1ServiceSpec()
            {
                Ports = new List<V1ServicePort>()
                {
                    new V1ServicePort()
                    {
                        Name          = "http",
                        Protocol      = "TCP",
                        Port          = 6333,
                        TargetPort    = 6333
                    }
                },
                Selector = new Dictionary<string, string>()
                {
                    { "foo", "bar" }
                },
                Type = "ClusterIP",
                InternalTrafficPolicy = "Cluster"

            };
            fixture.AddResource<V1Service>(service);

            var serviceList = await fixture.KubernetesClient.CoreV1.ListNamespacedServiceAsync(service.Metadata.NamespaceProperty);

            serviceList.Items.Should().HaveCount(1);

            service = await fixture.KubernetesClient.CoreV1.ReadNamespacedServiceAsync(name: service.Metadata.Name,
                namespaceParameter: service.Metadata.NamespaceProperty);

            service.Should().NotBeNull();
        }

        [Fact]
        public async Task TestCreateConfigMapAsync()
        {
            fixture.ClearResources();

            var configMap = new V1ConfigMap().Initialize();
            configMap.Metadata = new V1ObjectMeta()
            {
                Name = "test",
                NamespaceProperty = "test",
            };
            configMap.Data = new Dictionary<string, string>() { { "foo", "bar" } };

            await fixture.KubernetesClient.CoreV1.CreateNamespacedConfigMapAsync(configMap, configMap.Metadata.Name, configMap.Metadata.NamespaceProperty);

            var created = fixture.GetResource<V1ConfigMap>(configMap.Metadata.Name,configMap.Metadata.NamespaceProperty);
            created.Should().NotBeNull();
            created.Data.Should().ContainKey("foo");

            fixture.Resources.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestUpdateConfigMapAsync()
        {
            fixture.ClearResources();

            var configMap = new V1ConfigMap()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = "test",
                    NamespaceProperty = "test",
                },
                Data = new Dictionary<string, string>(){ { "foo", "bar" } }
            };

            fixture.AddResource<V1ConfigMap>(configMap);

            var config2 = new V1ConfigMap().Initialize();
            config2.Metadata.Name = "test-2";
            config2.Metadata.NamespaceProperty = "test";

            fixture.AddResource(config2);

            configMap.Data.Add("bar", "baz");

            await fixture.KubernetesClient.CoreV1.ReplaceNamespacedConfigMapAsync(configMap, configMap.Metadata.Name, configMap.Metadata.NamespaceProperty);

            var updated = fixture.GetResource<V1ConfigMap>(configMap.Metadata.Name,configMap.Metadata.NamespaceProperty);
            updated.Should().NotBeEquivalentTo(configMap);
            updated.Data.Should().ContainKey("foo");
            updated.Data.Should().ContainKey("bar");
            updated.Data["foo"].Should().Be("bar");
            updated.Data["bar"].Should().Be("baz");

            fixture.Resources.Should().HaveCount(2);
        }
        [Fact]
        public async Task TestPatchConfigMapAsync()
        {
            fixture.ClearResources();

            var configMap = new V1ConfigMap()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = "test",
                    NamespaceProperty = "test",
                },
                Data = new Dictionary<string, string>(){ { "foo", "bar" } }
            };

            fixture.AddResource<V1ConfigMap>(configMap);

            var config2 = new V1ConfigMap().Initialize();
            config2.Metadata.Name = "test-2";
            config2.Metadata.NamespaceProperty = "test";

            fixture.AddResource(config2);

            var patch = OperatorHelper.CreatePatch<V1ConfigMap>();

            patch.Add(path => path.Data["bar"], "baz");

            await fixture.KubernetesClient.CoreV1.PatchNamespacedConfigMapAsync(OperatorHelper.ToV1Patch<V1ConfigMap>(patch), configMap.Metadata.Name, configMap.Metadata.NamespaceProperty);

            var updated = fixture.GetResource<V1ConfigMap>(configMap.Metadata.Name,configMap.Metadata.NamespaceProperty);
            updated.Should().NotBeEquivalentTo(configMap);
            updated.Data.Should().ContainKey("foo");
            updated.Data.Should().ContainKey("bar");
            updated.Data["foo"].Should().Be("bar");
            updated.Data["bar"].Should().Be("baz");

            fixture.Resources.Should().HaveCount(2);
        }

        [Fact]
        public async Task TestDeleteConfigMapAsync()
        {
            fixture.ClearResources();

            var configMap = new V1ConfigMap()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = "test",
                    NamespaceProperty = "test",
                },
                Data = new Dictionary<string, string>(){ { "foo", "bar" } }
            };

            fixture.AddResource<V1ConfigMap>(configMap);

            var config2 = new V1ConfigMap().Initialize();
            config2.Metadata.Name = "test-2";
            config2.Metadata.NamespaceProperty = "test";

            fixture.AddResource(config2);

            await fixture.KubernetesClient.CoreV1.DeleteNamespacedConfigMapAsync(configMap.Metadata.Name, configMap.Metadata.NamespaceProperty);

            var deleted = fixture.GetResource<V1ConfigMap>(configMap.Metadata.Name,configMap.Metadata.NamespaceProperty);
            deleted.Should().BeNull();

            fixture.Resources.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestDeleteStatefulSetAsync()
        {
            fixture.ClearResources();

            var statefulSet = new V1StatefulSet().Initialize();
            statefulSet.Metadata.Name = "test";
            statefulSet.Metadata.NamespaceProperty = "test-ns";
            statefulSet.Spec = new V1StatefulSetSpec()
            {
                Replicas = 1
            };

            fixture.AddResource(statefulSet);

            await fixture.KubernetesClient.AppsV1.DeleteNamespacedStatefulSetAsync(statefulSet.Metadata.Name, statefulSet.Metadata.NamespaceProperty);

            var deleted = fixture.GetResource<V1StatefulSet>(statefulSet.Metadata.Name,statefulSet.Metadata.NamespaceProperty);
            deleted.Should().BeNull();

            fixture.Resources.Should().HaveCount(0);
        }

        [Fact]
        public async Task TestDeleteServiceAsync()
        {
            fixture.ClearResources();

            var service = new V1Service()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = "test",
                    NamespaceProperty = "test",
                },
                Spec = new V1ServiceSpec()
                {
                    Ports = [
                        new V1ServicePort()
                        {
                            Name = "foo",
                            Port = 1000
                        }
                    ]
                }
            };

            fixture.AddResource<V1Service>(service);

            var service2 = new V1Service().Initialize();
            service2.Metadata.Name = "test-2";
            service2.Metadata.NamespaceProperty = "test";

            fixture.AddResource(service2);

            await fixture.KubernetesClient.CoreV1.DeleteNamespacedServiceAsync(service.Metadata.Name, service.Metadata.NamespaceProperty);

            var deleted = fixture.GetResource<V1Service>(service.Metadata.Name,service.Metadata.NamespaceProperty);
            deleted.Should().BeNull();

            fixture.Resources.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestCreateJob()
        {
            fixture.ClearResources();

            var job = new V1Job().Initialize();
            job.Metadata.Name = "test";
            job.Metadata.NamespaceProperty = "test";
            job.Spec = new V1JobSpec()
            {
                Template = new V1PodTemplateSpec()
                {
                    Spec = new V1PodSpec()
                    {
                        Containers =
                        [
                            new V1Container()
                            {
                                Name  = "foo",
                                Image = "bar"
                            }
                        ]
                    }
                }
            };

            await fixture.KubernetesClient.BatchV1.CreateNamespacedJobAsync(job, job.Metadata.NamespaceProperty);
        }
    }
}

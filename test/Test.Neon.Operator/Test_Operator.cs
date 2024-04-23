//-----------------------------------------------------------------------------
// FILE:	    TestOperator.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using k8s;
using k8s.Models;

using Neon.K8s;
using Neon.K8s.Core;
using Neon.Operator.Util;
using Neon.Operator.Xunit;
using Neon.Xunit;

using Xunit;

namespace Test.Neon.Operator
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class TestOperator : IClassFixture<OperatorFixture>
    {
        private OperatorFixture fixture;

        public TestOperator(OperatorFixture fixture)
        {
            this.fixture = fixture;
            fixture.Operator.AddController<TestResourceController>();
            fixture.Operator.AddController<TestDatabaseController>();
            fixture.RegisterType<V1TestChildResource>();
            fixture.RegisterType<V1TestDatabase>();
            fixture.RegisterType<V1StatefulSet>();
            fixture.RegisterType<V1Service>();
            fixture.Start();
        }

        [Fact]
        public async Task CreateTestObjectAsync()
        {
            fixture.ClearResources();

            var controller = fixture.Operator.GetController<TestResourceController>();

            var resource = new V1TestResource();
            resource.Spec = new TestSpec()
            {
                Message = "I'm the parent object"
            };

            await controller.ReconcileAsync(resource);

            Assert.Contains(fixture.Resources, r => r.Metadata.Name == "child-object");

            Assert.Single(fixture.Resources);
        }

        [Fact]
        public async Task CreateStatefulSetAsync()
        {
            fixture.ClearResources();

            var controller = fixture.Operator.GetController<TestDatabaseController>();

            var resource = new V1TestDatabase()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name              = "test-database",
                    NamespaceProperty = "test"
                },
                Spec = new TestDatabaseSpec()
                {
                    Image      = "foo/bar:latest",
                    Servers    = 3,
                    VolumeSize = "1Gi"
                }
            };

            fixture.AddResource<V1TestDatabase>(resource);

            await controller.ReconcileAsync(resource);

            fixture.Resources.Count.Should().Be(3);

            var updatedResource = fixture.GetResource<V1TestDatabase>(resource.Name(), resource.Namespace());
            updatedResource.Status.Status.Should().Be("reconciled");

            // verify statefulset
            var statefulSet = fixture.GetResource<V1StatefulSet>(resource.Name(), resource.Namespace());

            statefulSet.Should().NotBeNull();
            statefulSet.Spec.Replicas.Should().Be(resource.Spec.Servers);

            // verify service
            var service     = fixture.GetResource<V1Service>(resource.Name(), resource.Namespace());
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task TestApiResourceList()
        {
            fixture.ClearResources();

            var meta = typeof(V1TestDatabase).GetKubernetesTypeMetadata();
            var resourceList = await fixture.KubernetesClient.CustomObjects.GetAPIResourcesAsync(meta.Group, meta.ApiVersion);
            resourceList.Resources.Should().HaveCount(3);

            meta = typeof(V1StatefulSet).GetKubernetesTypeMetadata();
            resourceList = await fixture.KubernetesClient.CustomObjects.GetAPIResourcesAsync(meta.Group, meta.ApiVersion);
            resourceList.Resources.Should().HaveCount(1);

            resourceList = await fixture.KubernetesClient.CoreV1.GetAPIResourcesAsync();
            resourceList.Resources.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestPatchStatus()
        {
            fixture.ClearResources();

            var controller = fixture.Operator.GetController<TestDatabaseController>();

            var resource = new V1TestDatabase()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name              = "test-database",
                    NamespaceProperty = "test"
                },
                Spec = new TestDatabaseSpec()
                {
                    Image      = "foo/bar:latest",
                    Servers    = 3,
                    VolumeSize = "1Gi"
                },
                Status = new TestDatabaseStatus()
                {
                    Status = "test"
                }
            };

            fixture.AddResource<V1TestDatabase>(resource);

            resource = KubernetesHelper.JsonClone(resource);

            resource.Status ??= new TestDatabaseStatus();
            resource.Status.Conditions ??= new List<V1Condition>();

            var condition = new V1Condition()
            {
                Type = "alert",
                Status = "true",
                LastTransitionTime = DateTime.UtcNow,
            };

            resource.Status.Conditions = resource.Status.Conditions.Where(c => c.Type != condition.Type).ToList();
            resource.Status.Conditions.Add(condition);

            var patch = OperatorHelper.CreatePatch<V1TestDatabase>();

            patch.Replace(path => path.Status.Conditions, resource.Status.Conditions);

            resource = await fixture.KubernetesClient.CustomObjects.PatchNamespacedCustomObjectStatusAsync<V1TestDatabase>(
                patch: OperatorHelper.ToV1Patch<V1TestDatabase>(patch),
                name: resource.Name(),
                namespaceParameter: resource.Namespace());

            resource.Status.Conditions.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestPatchNullStatus()
        {
            fixture.ClearResources();

            var controller = fixture.Operator.GetController<TestDatabaseController>();

            var resource = new V1TestDatabase()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name              = "test-database",
                    NamespaceProperty = "test"
                },
                Spec = new TestDatabaseSpec()
                {
                    Image      = "foo/bar:latest",
                    Servers    = 3,
                    VolumeSize = "1Gi"
                },
            };

            fixture.AddResource<V1TestDatabase>(resource);

            resource = KubernetesHelper.JsonClone(resource);

            var patch = OperatorHelper.CreatePatch<V1TestDatabase>();

            if (resource.Status == null)
            {
                resource.Status = new TestDatabaseStatus();
                patch.Replace(path => path.Status, new TestDatabaseStatus());
            }

            resource.Status.Conditions ??= new List<V1Condition>();

            var condition = new V1Condition()
            {
                Type = "alert",
                Status = "true",
                LastTransitionTime = DateTime.UtcNow,
            };

            resource.Status.Conditions = resource.Status.Conditions.Where(c => c.Type != condition.Type).ToList();
            resource.Status.Conditions.Add(condition);

            patch.Replace(path => path.Status.Conditions, resource.Status.Conditions);

            resource = await fixture.KubernetesClient.CustomObjects.PatchNamespacedCustomObjectStatusAsync<V1TestDatabase>(
                patch: OperatorHelper.ToV1Patch<V1TestDatabase>(patch),
                name: resource.Name(),
                namespaceParameter: resource.Namespace());

            resource.Status.Conditions.Should().HaveCount(1);

        }

        [Fact]
        public async Task TechPatchDictionary()
        {
            fixture.ClearResources();

            var controller = fixture.Operator.GetController<TestDatabaseController>();

            var resource = new V1TestDatabase()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name              = "test-database",
                    NamespaceProperty = "test"
                },
                Spec = new TestDatabaseSpec()
                {
                    Image      = "foo/bar:latest",
                    Servers    = 3,
                    VolumeSize = "1Gi"
                },
            };

            fixture.AddResource<V1TestDatabase>(resource);

            resource = KubernetesHelper.JsonClone(resource);

            var patch = OperatorHelper.CreatePatch<V1TestDatabase>();

            if (resource.Status == null)
            {
                resource.Status = new TestDatabaseStatus();
                patch.Replace(path => path.Status, new TestDatabaseStatus());
            }

            resource.Status.Conditions ??= new List<V1Condition>();

            var condition = new V1Condition()
            {
                Type = "alert",
                Status = "true",
                LastTransitionTime = DateTime.UtcNow,
            };

            if (resource.Status.DictValues == null)
            {
                resource.Status.DictValues = new Dictionary<string, V1Condition>();
            }

            resource.Status.DictValues["foo"] = condition;

            patch.Replace(path => path.Status.DictValues, resource.Status.DictValues);

            resource = await fixture.KubernetesClient.CustomObjects.PatchNamespacedCustomObjectStatusAsync<V1TestDatabase>(
                patch: OperatorHelper.ToV1Patch<V1TestDatabase>(patch),
                name: resource.Name(),
                namespaceParameter: resource.Namespace());

            resource.Status.DictValues.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestStatusUpdate()
        {
            fixture.ClearResources();

            var co = new V1TestDatabase().Initialize();
            co.Metadata.Name = "test";
            co.Metadata.NamespaceProperty = "test";
            co.Spec = new TestDatabaseSpec()
            {
                Image = "",
                Servers = 1,
            };
            co.Status = new TestDatabaseStatus()
            {
                Status = "foo"
            };

            fixture.AddResource(co);

            co = KubernetesHelper.JsonClone(co);
            co.Status.Status = "bar";

            var meta = typeof(V1TestDatabase).GetKubernetesTypeMetadata();
            await fixture.KubernetesClient.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync(co, co.Namespace());

            fixture.GetResource<V1TestDatabase>(co.Name(), co.Namespace()).Status.Status.Should().Be("bar");
        }

        [Fact]
        public async Task TestNullStatusUpdate()
        {
            fixture.ClearResources();

            var co = new V1TestDatabase().Initialize();
            co.Metadata.Name = "test";
            co.Metadata.NamespaceProperty = "test";
            co.Spec = new TestDatabaseSpec()
            {
                Image = "",
                Servers = 1,
            };

            fixture.AddResource(co);

            co = KubernetesHelper.JsonClone(co);
            co.Status = new TestDatabaseStatus();

            co.Status.Status = "bar";

            var meta = typeof(V1TestDatabase).GetKubernetesTypeMetadata();
            await fixture.KubernetesClient.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync(co, co.Namespace());

            fixture.GetResource<V1TestDatabase>(co.Name(), co.Namespace()).Status.Status.Should().Be("bar");
        }
    }
}
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

using System.Threading.Tasks;

using FluentAssertions;

using k8s.Models;

using Neon.Operator.Xunit;

using Xunit;

namespace Test.Neon.Operator
{
    public class TestOperator : IClassFixture<TestOperatorFixture>
    {
        private TestOperatorFixture fixture;

        public TestOperator(TestOperatorFixture fixture)
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
    }
}
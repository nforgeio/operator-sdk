// -----------------------------------------------------------------------------
// FILE:	    Test_DependencyInjection.cs
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
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using k8s;
using k8s.Models;

using Microsoft.Extensions.DependencyInjection;

using Neon.Operator.Controllers;
using Neon.Operator.Xunit;
using Neon.Xunit;

using Test.Neon.Operator;

using Xunit;

namespace TestKubeOperator
{
    public class Foo
    {
        public string Bar { get; set; }
        public Foo(string bar)
        {
            this.Bar = bar;
        }
    }
    public class TestDiController : ResourceControllerBase<V1TestResource>
    {
        public IKubernetes K8s;
        public Foo Foo;

        public TestDiController(
            IKubernetes k8s,
            Foo         foo)
        {
            ArgumentNullException.ThrowIfNull(k8s, nameof(k8s));
            ArgumentNullException.ThrowIfNull(foo, nameof(foo));

            this.K8s = k8s;
            this.Foo = foo;
        }

        public override async Task<ResourceControllerResult> ReconcileAsync(V1TestResource entity, CancellationToken cancellationToken = default)
        {
            var config = await K8s.CoreV1.ReadNamespacedConfigMapAsync(name: "foo", namespaceParameter: "bar");

            await base.ReconcileAsync(entity);

            return ResourceControllerResult.Ok();
        }
    }

    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class Test_DependencyInjection : IClassFixture<OperatorFixture>, IDisposable
    {
        private OperatorFixture fixture;

        public Test_DependencyInjection(OperatorFixture fixture)
        {
            this.fixture = fixture;
            this.fixture.Services.AddSingleton<Foo>(new Foo("bar"));
            this.fixture.Operator.AddController<TestDiController>();
            this.fixture.RegisterType<V1ConfigMap>();
            this.fixture.Start();
        }

        [Fact]
        public async Task FooExists()
        {
            var controller = fixture.Operator.GetController<TestDiController>();

            var config = new V1ConfigMap().Initialize();

            config.Metadata.Name = "foo";
            config.Metadata.NamespaceProperty = "bar";
            config.Data = new Dictionary<string, string>();
            config.Data.Add("foo", "bar");

            fixture.AddResource(config);

            var resource = new V1TestResource();
            resource.Spec = new TestSpec()
            {
                Message = "I'm the parent object"
            };

            var result = await controller.ReconcileAsync(resource);

            result.Should().Be(null);
            controller.Foo.Should().NotBeNull();
            controller.Foo.Bar.Should().Be("bar");
        }

        public void Dispose() { }
    }
}

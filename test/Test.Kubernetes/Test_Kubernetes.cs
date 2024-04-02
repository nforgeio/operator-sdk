// -----------------------------------------------------------------------------
// FILE:	    Test_Kubernetes.cs
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

#if JUSTMOCK

using System.Threading.Tasks;

using FluentAssertions;

using k8s;
using k8s.Models;

using Neon.K8s;
using Neon.K8s.Resources.CertManager;
using Neon.Operator.Xunit;
using Neon.Xunit;

namespace TestKubernetes
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class Test_Kubernetes : IClassFixture<TestOperatorFixture>
    {
        private TestOperatorFixture fixture;

        public Test_Kubernetes(TestOperatorFixture fixture)
        {
            this.fixture = fixture;
            fixture.RegisterType<V1Certificate>();
            fixture.Start();
        }

        [Fact]
        public async Task TestGetCustomObjectReturnsNull()
        {
            var cert = new V1Certificate().Initialize();
            cert.EnsureMetadata();
            cert.Metadata.Name = "test-cert";
            cert.Metadata.NamespaceProperty = "test-namespace";

            cert.Spec = new V1CertificateSpec()
            {
                Duration = "1h"
            };

            var result = await fixture.KubernetesClient.CustomObjects.GetNamespacedCustomObjectAsync<V1Certificate>(cert.Name(), cert.Namespace(), throwIfNotFound: false);

            result.Should().BeNull();

            fixture.AddResource(cert);

            result = await fixture.KubernetesClient.CustomObjects.GetNamespacedCustomObjectAsync<V1Certificate>(cert.Name(), cert.Namespace(), throwIfNotFound: false);

            result.Should().NotBeNull();
        }
    }
}

#endif // JUSTMOCK

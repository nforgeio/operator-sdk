// -----------------------------------------------------------------------------
// FILE:	    UnitTest1.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2025 by NEONFORGE LLC.  All rights reserved.
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

using FluentAssertions;

using Neon.K8s;
using Neon.K8s.Models;
using Neon.Xunit;

namespace TestKubernetesCore
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class Test_V1Status
    {
        [Fact]
        public void Test1()
        {
            var v1Status = new V1Status { Message = "test message", Status = "test status" };

            var json = KubernetesJson.Serialize(v1Status);

            json.Should().Be($@"""test message""");
        }

        [Fact]
        public void TestV1Namespace()
        {
            var corev1Namespace = new V1Namespace()
            {
                Metadata = new V1ObjectMeta() { Name = "test name" },
                Status = new V1NamespaceStatus() { Phase = "test termating" },
            };

            var json = KubernetesJson.Serialize(corev1Namespace);

            json.Should().Be($@"{{""metadata"":{{""name"":""test name""}},""status"":{{""phase"":""test termating""}}}}");
        }
    }
}
// FILE:	    TestOperatorFixture.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

using System.Collections.Generic;
using System.Linq;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Hosting;

using Neon.K8s;
using Neon.Xunit;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// A test fixture used for testing Kubernetes Operators.
    /// </summary>
    public class TestOperatorFixture : TestFixture
    {
        /// <summary>
        /// The operator under test.
        /// </summary>
        public ITestOperator Operator { get; set; }

        /// <summary>
        /// Kubernetes client used for interacting with the API server.
        /// </summary>
        public IKubernetes KubernetesClient { get; private set; }

        /// <summary>
        /// The Kubernetes configuration for the test api server.
        /// </summary>
        public KubernetesClientConfiguration KubernetesClientConfiguration { get; private set; }

        /// <summary>
        /// The API server resource collection.
        /// </summary>
        public List<IKubernetesObject<V1ObjectMeta>> Resources => testApiServerHost.Cluster.Resources;
        
        private ITestApiServerHost testApiServerHost;
        private bool started;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TestOperatorFixture()
        {
            this.testApiServerHost = new TestApiServerBuilder()
                .Build();

            this.KubernetesClientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigObject(testApiServerHost.KubeConfig);

            this.KubernetesClient = new Kubernetes(KubernetesClientConfiguration, new KubernetesRetryHandler());
            this.Operator = new TestOperator(KubernetesClientConfiguration);
        }

        /// <summary>
        /// Start the test fixture.
        /// </summary>
        /// <returns>The <see cref="TestFixtureStatus"/>.</returns>
        public TestFixtureStatus Start()
        {
            if (started) 
            {
                return TestFixtureStatus.AlreadyRunning;
            }

            testApiServerHost.Start();

            Operator.Start();

            started = true;

            return TestFixtureStatus.Started;
        }

        public void RegisterType<T>()
            where T : IKubernetesObject<V1ObjectMeta>
        {
            var typeMetadata = typeof(T).GetKubernetesTypeMetadata();

            var key = $"{typeMetadata.Group}/{typeMetadata.ApiVersion}/{typeMetadata.PluralName}";
            testApiServerHost.Cluster.Types.TryAdd(key, typeof(T));
        }

        public void AddResource<T>(T resource, string namespaceParameter = null)
            where T : IKubernetesObject<V1ObjectMeta>
        {
            testApiServerHost.Cluster.AddResource<T>(resource, namespaceParameter);
        }

        public IEnumerable<T> GetResources<T>(string namespaceParameter = null)
        {
            var query = this.Resources.AsQueryable();

            if (namespaceParameter != null)
            {
                query = query.Where(r => r.EnsureMetadata().NamespaceProperty == namespaceParameter);
            }

            return query.OfType<T>();
        }

        public T GetResource<T>(string name, string namespaceParameter = null)
        {
            var query = this.Resources.AsQueryable();

            if (namespaceParameter != null)
            {
                query = query.Where(r => r.EnsureMetadata().NamespaceProperty == namespaceParameter);
            }

            return query.Where(r => r.EnsureMetadata().Name == name).OfType<T>().FirstOrDefault();
        }

        public void ClearResources()
        {
            testApiServerHost.Cluster.Resources.Clear();
        }
    }
}

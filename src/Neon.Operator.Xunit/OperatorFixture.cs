// FILE:	    OperatorFixture.cs
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

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

using k8s;
using k8s.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neon.K8s;
using Neon.Xunit;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// A test fixture used for testing Kubernetes Operators.
    /// </summary>
    public class OperatorFixture : TestFixture
    {
        private ITestApiServerHost   testApiServerHost;
        private bool                 started;

        /// <summary>
        /// Constructor.
        /// </summary>
        public OperatorFixture()
        {
            this.testApiServerHost             = new TestApiServerBuilder().Build();
            this.KubernetesClientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigObject(testApiServerHost.KubeConfig);
            this.KubernetesClient              = new Kubernetes(KubernetesClientConfiguration, new KubernetesRetryHandler());
            this.Operator                      = new TestOperator(KubernetesClientConfiguration);
        }

        /// <summary>
        /// Returns the operator under test.
        /// </summary>
        public ITestOperator Operator { get; private set; }

        /// <summary>
        /// Returns the Kubernetes client used for interacting with the API server.
        /// </summary>
        public IKubernetes KubernetesClient { get; private set; }

        /// <summary>
        /// Returns the Kubernetes configuration for the test api server.
        /// </summary>
        public KubernetesClientConfiguration KubernetesClientConfiguration { get; private set; }

        /// <summary>
        /// Returns the API server resource collection.
        /// </summary>
        public List<IKubernetesObject<V1ObjectMeta>> Resources => testApiServerHost.Cluster.Resources;

        /// <summary>
        /// Returns the operator's service collection.
        /// </summary>
        public IServiceCollection Services => Operator.Services;

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

        /// <summary>
        /// Registers a custom resource definition type with the emulated cluster,
        /// </summary>
        /// <typeparam name="T">Specifies the custom resource type.</typeparam>
        public void RegisterType<T>()
            where T : IKubernetesObject<V1ObjectMeta>
        {
            var typeMetadata = typeof(T).GetKubernetesTypeMetadata();
            var key          = $"{typeMetadata.Group}/{typeMetadata.ApiVersion}/{typeMetadata.PluralName}";

            testApiServerHost.Cluster.Types.TryAdd(key, typeof(T));
        }

        /// <summary>
        /// Adds a custom resource with the emulated cluster.
        /// </summary>
        /// <typeparam name="T">Specifies the custom resource type for the new resource.</typeparam>
        /// <param name="resource">Specifies the new resource.</param>
        /// <param name="namespaceParameter">Optionally specifies the resource namespace for the non-namespaced resources.</param>
        public void AddResource<T>(T resource, string namespaceParameter = null)
            where T : IKubernetesObject<V1ObjectMeta>
        {
            Covenant.Requires<ArgumentNullException>(resource != null, nameof(resource));

            testApiServerHost.Cluster.AddResource<T>(resource, namespaceParameter);
        }

        /// <summary>
        /// Returns custom resources of the specified type from the emulated cluster.
        /// </summary>
        /// <typeparam name="T">Specifies the custom resource type.</typeparam>
        /// <param name="namespaceParameter">Optionally specifies the namespace for namespaced resources.</param>
        /// <returns>The resources found.</returns>
        public IEnumerable<T> GetResources<T>(string namespaceParameter = null)
        {
            var query = this.Resources.AsQueryable();

            if (namespaceParameter != null)
            {
                query = query.Where(resource => resource.EnsureMetadata().NamespaceProperty == namespaceParameter);
            }

            return query.OfType<T>();
        }

        /// <summary>
        /// Returns a specific resource from the emulated cluster.
        /// </summary>
        /// <typeparam name="T">Specifies the custom resource type.</typeparam>
        /// <param name="name">Specifies the resource name.</param>
        /// <param name="namespaceParameter">Optionally specifies the namespace for namespaced resources.</param>
        /// <returns>The resource found or <c>null</c> when it dcoesn't exist.</returns>
        public T GetResource<T>(string name, string namespaceParameter = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));

            var query = this.Resources.AsQueryable();

            if (namespaceParameter != null)
            {
                query = query.Where(r => r.EnsureMetadata().NamespaceProperty == namespaceParameter);
            }

            return query.Where(resource => resource.EnsureMetadata().Name == name)
                .OfType<T>()
                .SingleOrDefault();
        }

        /// <summary>
        /// Clears all resources from the emulated cluster.
        /// </summary>
        public void ClearResources()
        {
            testApiServerHost.Cluster.Resources.Clear();
        }
    }
}

// FILE:	    TestApiServerBuilder.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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

using System.Net;

using k8s;
using k8s.KubeConfigModels;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neon.Net;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Builds an <see cref="ITestApiServerHost"/>
    /// </summary>
    public class TestApiServerBuilder
    {
        //internal IServiceCollection Services { get; set; } = new ServiceCollection();

        internal readonly IHostBuilder hostBuilder = new HostBuilder();

        /// <summary>
        /// Builds an <see cref="ITestApiServerHost"/>
        /// </summary>
        /// <returns>The <see cref="ITestApiServerHost"/>.</returns>
        public ITestApiServerHost Build()
        {
            if (string.IsNullOrEmpty(ServerUrl))
            {
                ServerUrl = $"http://{IPAddress.Loopback}:{NetHelper.GetUnusedTcpPort()}";
            }

            hostBuilder.ConfigureWebHostDefaults(web =>
            {
                web.UseStartup<TestApiServerStartup>()
                   .UseUrls(ServerUrl);
            });

            //hostBuilder.ConfigureServices((ctx) =>
            //{
            //    foreach (var svc in Services)
            //    {
            //        ctx.Add(svc);
            //    }
            //});

            var host = hostBuilder.Build();

            var kubeConfig = new K8SConfiguration
            {
                ApiVersion     = "v1",
                Kind           = "Config",
                CurrentContext = "test-context",
                Contexts       = new[]
                {
                    new Context
                    {
                        Name           = "test-context",
                        ContextDetails = new ContextDetails
                        {
                            Namespace = "test-namespace",
                            Cluster   = "test-cluster",
                            User      = "test-user",
                        }
                    }
                },
                Clusters = new[]
                {
                    new Cluster
                    {
                        Name            = "test-cluster",
                        ClusterEndpoint = new ClusterEndpoint
                        {
                            Server = ServerUrl,
                        }
                    }
                },
                Users = new[]
                {
                    new User
                    {
                        Name            = "test-user",
                        UserCredentials = new UserCredentials
                        {
                            Token = "test-token",
                        }
                    }
                },
            };

            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigObject(kubeConfig);
            var client              = new k8s.Kubernetes(clientConfiguration);

            return new TestApiServerHost(host, kubeConfig, client);
        }

        /// <summary>
        /// The server URL.
        /// </summary>
        public string ServerUrl { get; set; }
    }
}

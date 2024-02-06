// FILE:	    ITestApiServerHost.cs
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

using k8s;
using k8s.KubeConfigModels;

using Microsoft.Extensions.Hosting;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Test API server <see cref="IHost"/>.
    /// </summary>
    public interface ITestApiServerHost : IHost
    {
        /// <summary>
        /// The k8s config for the test API server.
        /// </summary>
        K8SConfiguration KubeConfig { get; }

        /// <summary>
        /// The kubernetes client for interacting with the test API server.
        /// </summary>
        IKubernetes K8s { get; }

        /// <summary>
        /// The test API server.
        /// </summary>
        ITestApiServer Cluster { get; }
    }
}

//-----------------------------------------------------------------------------
// FILE:	    IResourceManager.cs
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

using System.Threading;
using System.Threading.Tasks;

namespace Neon.Operator.ResourceManager
{
    /// <summary>
    /// Describes a resource manager.
    /// </summary>
    internal interface IResourceManager
    {
        /// <summary>
        /// Returns the resource manager's options.
        /// </summary>
        ResourceManagerOptions Options();

        /// <summary>
        /// Starts the resource manager.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);
    }
}

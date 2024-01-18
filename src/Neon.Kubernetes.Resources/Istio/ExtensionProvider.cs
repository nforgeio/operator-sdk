//-----------------------------------------------------------------------------
// FILE:        ExtensionProvider.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// Identifies an Extension Provider.
    /// </summary>
    public class ExtensionProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ExtensionProvider()
        {

        }

        /// <summary>
        /// <para>
        /// Specifies the name of the extension provider. The list of available providers is defined in the MeshConfig. 
        /// Note, currently at most 1 extension provider is allowed per workload. Different workloads can use different 
        /// extension provider.
        /// </para>
        /// </summary>
        public string Name { get; set; } = null;
    }
}

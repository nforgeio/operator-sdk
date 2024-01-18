//-----------------------------------------------------------------------------
// FILE:        AcmeSecretKeySelector.cs
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

using System.ComponentModel;

using YamlDotNet.Serialization;

namespace Neon.K8s.Resources.CertManager
{
    /// <summary>
    /// Describes CertManager Secret Key Selector.
    /// </summary>
    public class AcmeSecretKeySelector
    {
        //---------------------------------------------------------------------
        // Implementation

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AcmeSecretKeySelector()
        {
        }

        /// <summary>
        /// The key of the entry in the Secret resource’s data field to be used. Some instances of this field may be defaulted, 
        /// in others it may be required.
        /// </summary>
        [YamlMember(Alias = "key", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Key { get; set; } = null;

        /// <summary>
        /// Name of the resource being referred to. More info: https://kubernetes.io/docs/concepts/overview/working-with-objects/names/#names
        /// </summary>
        [YamlMember(Alias = "name", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Name { get; set; } = null;

        /// <inheritdoc/>
        public void Validate() { }
    }
}

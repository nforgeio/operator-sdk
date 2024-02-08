// -----------------------------------------------------------------------------
// FILE:	    SpecDescriptor.cs
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
using System.ComponentModel.DataAnnotations;
using System.Text;

using YamlDotNet.Serialization;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// Descriptor describes a field in a spec block of a CRD so that OLM can consume it
    /// </summary>
    public class Descriptor
    {
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// path
        /// </summary>
        [Required]
        public string Path { get; set; }

        /// <summary>
        /// RawMessage is a raw encoded JSON value.
        /// It implements Marshaler and Unmarshaler and can be used to delay JSON decoding or precompute a JSON encoding.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// x- descriptors
        /// </summary>
        [YamlMember(Alias = "x-descriptors", ApplyNamingConventions = false)]
        public List<string> XDescriptors { get; set; }
    }
}

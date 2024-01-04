﻿//-----------------------------------------------------------------------------
// FILE:	    KubernetesObjectMetadata.cs
// CONTRIBUTOR: Jeff Lill
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

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

using k8s;
using k8s.Models;

using Neon.Common;

namespace Neon.K8s
{
    /// <summary>
    /// Describes a Kubernetes object by its basic properties, <see cref="ApiVersion"/>, <see cref="Kind"/>, and <see cref="Metadata"/>.
    /// </summary>
    public class KubernetesObjectMetadata : IKubernetesObject<V1ObjectMeta>, IMetadata<V1ObjectMeta>, IValidate
    {
        /// <inheritdoc/>
        public string ApiVersion { get; set; }

        /// <inheritdoc/>
        public string Kind { get; set; }

        /// <inheritdoc/>
        public V1ObjectMeta Metadata { get; set; }

        /// <inheritdoc/>
        public void Validate()
        {
        }
    }
}

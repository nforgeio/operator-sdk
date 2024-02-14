// -----------------------------------------------------------------------------
// FILE:	    ApiResourceReference.cs
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
using System.Text;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// APIResourceReference is a reference to a Kubernetes resource type that the referrer utilizes.
    /// </summary>
    public class ApiResourceReference
    {
        /// <summary>
        /// Kind of the referenced resource type.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Plural name of the referenced resource type
        /// (CustomResourceDefinition.Spec.Names[].Plural).
        /// Empty string if the referenced resource type is not a custom resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// API Version of the referenced resource type.
        /// </summary>
        public string Version { get; set; }
    }
}

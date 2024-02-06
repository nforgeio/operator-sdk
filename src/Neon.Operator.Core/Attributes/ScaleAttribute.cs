﻿// -----------------------------------------------------------------------------
// FILE:	    ScaleAttribute.cs
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

namespace Neon.Operator.Attributes
{
    /// <summary>
    /// Indicates whether the defined custom resource is cluster- or namespace-scoped. 
    /// Allowed values are `Cluster` and `Namespaced`.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ScaleAttribute : Attribute
    {
        /// <summary>
        /// Defines the JSONPath inside of a custom resource that corresponds to Scale.Spec.Replicas.
        /// </summary>
        public string SpecReplicasPath { get; set; }

        /// <summary>
        /// Defines the JSONPath inside of a custom resource that corresponds to Scale.Status.Replicas.
        /// </summary>
        public string StatusReplicasPath { get; set; }


        /// <summary>
        /// Defines the JSONPath inside of a custom resource that corresponds to Scale.Status.Selector.
        /// </summary>
        public string LabelSelectorPath { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScaleAttribute()
        {
        }
    }
}

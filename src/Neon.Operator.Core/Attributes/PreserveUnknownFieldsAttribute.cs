// -----------------------------------------------------------------------------
// FILE:	    PreserveUnknownFieldsAttribute.cs
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
    /// By default, all unspecified fields for a custom resource, across all versions, are pruned.
    /// It is possible though to opt-out of that for specifc sub-trees of fields by adding
    /// x-kubernetes-preserve-unknown-fields: true in the structural OpenAPI v3 validation schema.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class PreserveUnknownFieldsAttribute : Attribute
    {
        /// <summary>
        /// Whether to enable preservation of unknown fields.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}

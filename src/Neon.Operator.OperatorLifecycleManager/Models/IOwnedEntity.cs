//-----------------------------------------------------------------------------
// FILE:	    IOwnedEntity.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Describes an Owned Resource.
    /// </summary>
    public interface IOwnedEntity
    {
        /// <summary>
        /// Name is the metadata.name
        /// of the CRD (which is of the form plural.group)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Version is the spec.versions[].name value defined in the CRD
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Kind is the CamelCased singular value defined in spec.names.kind of the CRD.
        /// </summary>
        public string Kind { get; }

    }
}

// -----------------------------------------------------------------------------
// FILE:	    OwnedEntityAttribute.cs
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

namespace Neon.Operator.OperatorLifecycleManager
{

    /// <summary>
    /// Models an Owned Resource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class OwnedEntityAttribute : Attribute, IOwnedEntity
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Specifies the name</param>
        /// <param name="version">Specifies the version</param>
        /// <param name="kind">Specifies the kind</param>
        public OwnedEntityAttribute(string name,string version,string kind)
        {
            this.Name    = name;
            this.Version = version;
            this.Kind    = kind;
        }

        /// <summary>
        ///  Name is the metadata.name of the CRD, which is of the form (plural.group)
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Version is the spec.versions[].name value defined in the CRD
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Kind is the CamelCased singular value defined in spec.names.kind of the CRD.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Description of the CRD
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// DisplayName of the CRD
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Example Json of the CRD
        /// </summary>
        public string ExampleJson { get; set; }

        /// <summary>
        /// Example yaml of the CRD
        /// </summary>
        public string ExampleYaml { get; set; }
    }
}

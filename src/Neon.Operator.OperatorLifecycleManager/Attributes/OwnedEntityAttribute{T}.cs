// -----------------------------------------------------------------------------
// FILE:	    OwnedEntityAttribute{T}.cs
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
using System.Runtime.Serialization;

using k8s;
using k8s.Models;

using Neon.Operator.OperatorLifecycleManager;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Models an Owned Resource.
    /// </summary>
    /// <typeparam name="TEntity">Specifies the target entity type.</typeparam>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class OwnedEntityAttribute<TEntity> : Attribute, IOwnedEntity
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public OwnedEntityAttribute() { }

        /// <summary>
        ///  Name is the metadata.name of the CRD, which is of the form (plural.group)
        /// </summary>
        public string Name => $"{GetKubernetesEntityAttribute().PluralName}.{GetKubernetesEntityAttribute().Group}";


        /// <summary>
        /// Version is the spec.versions[].name value defined in the CRD
        /// </summary>
        public string Version => GetKubernetesEntityAttribute().ApiVersion;

        /// <summary>
        /// Kind is the CamelCased singular value defined in spec.names.kind of the CRD.
        /// </summary>
        public string Kind => GetKubernetesEntityAttribute().Kind;

        /// <summary>
        /// Description of the CRD
        /// </summary>
        public string Description {  get; set; }

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



        /// <inheritdoc/>
        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        /// <inheritdoc/>
        public KubernetesEntityAttribute GetKubernetesEntityAttribute()
        {
            return GetEntityType().GetKubernetesTypeMetadata();
        }
    }
}

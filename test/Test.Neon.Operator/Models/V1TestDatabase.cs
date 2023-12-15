//-----------------------------------------------------------------------------
// FILE:	    V1TestDatabase.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

using System.ComponentModel.DataAnnotations;

using k8s;
using k8s.Models;

using Neon.Operator.Attributes;

namespace Test.Neon.Operator
{
    /// <summary>
    /// Used for unit testing Kubernetes clients.
    /// </summary>
    [KubernetesEntity(Group = KubeGroup, ApiVersion = KubeApiVersion, Kind = KubeKind, PluralName = KubePlural)]
    [EntityScope(EntityScope.Namespaced)]
    public class V1TestDatabase : IKubernetesObject<V1ObjectMeta>, ISpec<TestDatabaseSpec>, IStatus<TestDatabaseStatus>, IValidate
    {
        /// <summary>
        /// Object API group.
        /// </summary>
        public const string KubeGroup = "test.neonkube.io";

        /// <summary>
        /// Object API version.
        /// </summary>
        public const string KubeApiVersion = "v1alpha1";

        /// <summary>
        /// Object API kind.
        /// </summary>
        public const string KubeKind = "TestDatabase";

        /// <summary>
        /// Object plural name.
        /// </summary>
        public const string KubePlural = "testdatabases";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public V1TestDatabase()
        {
            ApiVersion = $"{KubeGroup}/{KubeApiVersion}";
            Kind = KubeKind;
        }

        /// <inheritdoc/>
        public string ApiVersion { get; set; }

        /// <inheritdoc/>
        public string Kind { get; set; }

        /// <inheritdoc/>
        public V1ObjectMeta Metadata { get; set; }

        /// <inheritdoc/>
        public TestDatabaseSpec Spec { get; set; }

        /// <inheritdoc/>
        public TestDatabaseStatus Status { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }

    /// <summary>
    /// Database spec.
    /// </summary>
    public class TestDatabaseSpec
    {
        /// <summary>
        /// The container image.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// The number of pods to deploy.
        /// </summary>
        public int Servers { get; set; }

        /// <summary>
        /// The volume size
        /// </summary>
        public string VolumeSize { get; set; }
    }

    /// <summary>
    /// status.
    /// </summary>
    public class TestDatabaseStatus
    {
        public string Status { get; set; }
    }
}
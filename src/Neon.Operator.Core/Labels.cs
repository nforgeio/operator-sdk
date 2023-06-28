// -----------------------------------------------------------------------------
// FILE:	    Labels.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

namespace Neon.Operator
{
    /// <summary>
    /// <para>
    /// You can visualize and manage Kubernetes objects with more tools than kubectl and the dashboard. A common set of
    /// labels allows tools to work interoperably, describing objects in a common manner that all tools can understand.
    /// </para>
    /// <para>
    /// In addition to supporting tooling, the recommended labels describe applications in a way that can be queried.
    /// </para>
    /// <para>
    /// The metadata is organized around the concept of an application. Kubernetes is not a platform as a service (PaaS)
    /// and doesn't have or enforce a formal notion of an application. Instead, applications are informal and described
    /// with metadata. The definition of what an application contains is loose.
    /// </para>
    /// <note>
    /// These are recommended labels. They make it easier to manage applications but aren't required for any core tooling.
    /// </note>
    /// <para>
    /// Shared labels and annotations share a common prefix: app.kubernetes.io. Labels without a prefix are private to users.
    /// The shared prefix ensures that shared labels do not interfere with custom user labels.
    /// </para>
    /// </summary>
    public static class KubernetesLabel
    {
        /// <summary>
        /// The Kubernetes label prefix.
        /// </summary>
        public const string KubernetesPrefix = "app.kubernetes.io/";

        /// <summary>
        /// The name of the application.
        /// </summary>
        public const string Name = KubernetesPrefix + "name";

        /// <summary>
        /// A unique name identifying the instance of an application.
        /// </summary>
        public const string Instance = KubernetesPrefix + "instance";

        /// <summary>
        /// The current version of the application (e.g., a <see href="https://semver.org/spec/v1.0.0.html">SemVer 1.0</see>, revision hash, etc.).
        /// </summary>
        public const string Version = KubernetesPrefix + "version";

        /// <summary>
        /// The component within the architecture.
        /// </summary>
        public const string Component = KubernetesPrefix + "component";

        /// <summary>
        /// he name of a higher level application this one is part of.
        /// </summary>
        public const string PartOf = KubernetesPrefix + "part-of";

        /// <summary>
        /// The tool being used to manage the operation of an application.
        /// </summary>
        public const string ManagedBy = KubernetesPrefix + "managed-by";
    }
}
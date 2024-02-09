// -----------------------------------------------------------------------------
// FILE:	    APIServiceDescription.cs
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

using System.Collections.Generic;

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// APIServiceDescription provides details to OLM about apis provided via aggregation
    /// </summary>
    public class ApiServiceDescription
    {
        /// <summary>
        /// ActionDescriptor describes a declarative action that can be performed on a custom resource instance
        /// </summary>
        public List<Descriptor> ActionDescriptors { get; set; }

        /// <summary>
        /// container port
        /// </summary>
        public int ContainerPort { get; set; }

        /// <summary>
        /// deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Group
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Kind
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// APIResourceReference is a reference to a Kubernetes resource type that the referrer utilizes.
        /// </summary>
        public List<ApiResourceReference> Resources { get; set; }

        /// <summary>
        /// SpecDescriptor describes a field in a spec block of a CRD so that OLM can consume it
        /// </summary>
        public List<Descriptor> SpecDescriptors { get; set; }

        /// <summary>
        /// StatusDescriptor describes a field in a status block of a CRD so that OLM can consume it
        /// </summary>
        public List<Descriptor> StatusDescriptors { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        public string Version { get; set; }
    }
}

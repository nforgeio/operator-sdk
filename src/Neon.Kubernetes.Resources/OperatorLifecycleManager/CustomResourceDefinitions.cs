// -----------------------------------------------------------------------------
// FILE:	    ApiServiceDefinitions.cs
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

namespace Neon.Kubernetes.Resources.OperatorLifecycleManager
{
    /// <summary>
    /// CustomResourceDefinitions declares all of the CRDs managed or required
    /// by an operator being ran by ClusterServiceVersion. If the CRD is present in the Owned list, it is implicitly required.
    /// </summary>
    public class CustomResourceDefinitions
    {
        /// <summary>
        /// CrdDescription provides details to OLM about the CRDs
        /// </summary>
        public List<CrdDescription> Owned {  get; set; }

        /// <summary>
        /// CrdDescription provides details to OLM about the CRDs
        /// </summary>
        public List<CrdDescription> Required {  get; set; }
    }
}

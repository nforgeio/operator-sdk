// -----------------------------------------------------------------------------
// FILE:	    RelatedImages.cs
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
    /// List any related images, or other container images that your Operator
    /// might require to perform their functions. This list should also include
    /// operand images as well. All image references
    /// should be specified by digest (SHA) and not by tag. This field is only
    /// used during catalog creation and plays no part in cluster runtime.
    /// </summary>
    public class RelatedImages
    {
        /// <summary>
        /// image reference
        /// </summary>
        public string Image {  get; set; }

        /// <summary>
        /// name of the image
        /// </summary>
        public string Name { get; set; }

    }
}

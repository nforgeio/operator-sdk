// -----------------------------------------------------------------------------
// FILE:	    Ci.cs
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
using System.ComponentModel;
using System.Text;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// change the default updategraph mode or add reviewers
    /// </summary>
    public class Ci
    {
        /// <summary>
        /// update graph mode
        /// </summary>
        [DefaultValue(UpdateGraphMode.SemverMode)]
        public UpdateGraphMode UpdateGraph { get; set; } = UpdateGraphMode.SemverMode;

        /// <summary>
        /// reviewers flag
        /// </summary>
        [DefaultValue(false)]
        public bool AddReviewers { get; set; } = false;

        /// <summary>
        /// list of reviewers
        /// </summary>
        public List<string> Reviewers { get; set; } = new List<string>();

    }
}

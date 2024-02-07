// -----------------------------------------------------------------------------
// FILE:	    GroupVersionKind.cs
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
    /// GroupVersionKind unambiguously identifies a kind. It doesn’t anonymously
    /// include GroupVersion to avoid automatic coercion. It doesn’t use a GroupVersion to avoid custom marshalling
    /// </summary>
    public class GroupVersionKind
    {
        public string Group {  get; set; }
        public string Version { get; set; }
        public string Kind { get; set; }

    }
}

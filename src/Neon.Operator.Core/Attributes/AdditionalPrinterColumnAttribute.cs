//-----------------------------------------------------------------------------
// FILE:	    AdditionalPrinterColumnAttribute.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

using System;

namespace Neon.Operator.Attributes
{
    /// <summary>
    /// The kubectl tool relies on server-side output formatting. Your cluster's API server decides which columns 
    /// are shown by the kubectl get command. You can customize these columns for a CustomResourceDefinition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class AdditionalPrinterColumnAttribute : Attribute
    {
        /// <summary>
        /// The name of the column.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>
        /// Each column includes a priority field. Currently, the priority differentiates between columns shown in 
        /// standard view or wide view (using the -o wide flag).
        /// </para>
        /// <list type="bullet">
        /// <item>Columns with priority 0 are shown in standard view.</item>
        /// <item>Columns with priority greater than 0 are shown only in wide view.</item>
        /// </list>
        /// </summary>
        public int Priority { get; set; }
    }
}
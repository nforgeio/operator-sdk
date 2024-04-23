// -----------------------------------------------------------------------------
// FILE:	    AnalyzerHelper.cs
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace Neon.Operator.Analyzers
{
    /// <summary>
    /// Implements analuzer related helper methods.
    /// </summary>
    internal static class AnalyzerHelper
    {
        /// <summary>
        /// Writes the text passed to the file when the file doesn't exist or the
        /// text differs from the existing file.  This is used for writing generated
        /// files without burning out any local SSD.
        /// </summary>
        /// <param name="path">Specifies the file path.</param>
        /// <param name="text">Specifies the text to be written.</param>
        public static void WriteFileWhenDifferent(string path, string text)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path), nameof(path));
            Covenant.Requires<ArgumentNullException>(text != null, nameof(text));

            if (!File.Exists(path) || File.ReadAllText(path) != text)
            {
                File.WriteAllText(path, text);
            }
        }

        /// <summary>
        /// Writes the <see cref="StringBuilder"/>text passed to the file when the file doesn't
        /// exist or the text differs from the existing file.  This is used for writing generated
        /// files without burning out any local SSD.
        /// </summary>
        /// <param name="path">Specifies the file path.</param>
        /// <param name="sb">Specifies the <see cref="StringBuilder"/> holding the text to be written.</param>
        public static void WriteFileWhenDifferent(string path, StringBuilder sb)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path), nameof(path));
            Covenant.Requires<ArgumentNullException>(sb != null, nameof(sb));

            WriteFileWhenDifferent(path, sb.ToString());
        }
    }
}

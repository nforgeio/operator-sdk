// -----------------------------------------------------------------------------
// FILE:	    IconAttribute.cs
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
using System.IO;
using System.Text;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Specifies the icon for the Operator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class IconAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public IconAttribute() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mediaType"></param>
        public IconAttribute(string path, string mediaType = null)
        {
            this.Path = path;
            this.MediaType = mediaType;
        }

        /// <summary>
        /// Path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// MediaType
        /// </summary>
        public string MediaType { get; set; }

        /// <summary>
        /// Converts the IconAttribute to an Icon
        /// </summary>
        /// <param name="baseDir"></param>
        /// <returns></returns>
        public Icon ToIcon(string baseDir = null)
        {
            var path = Path;
            if (baseDir != null)
            {
                path = System.IO.Path.Combine(baseDir, path);
            }
            byte[] imageArray = System.IO.File.ReadAllBytes(path); // read the bytes
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            return new Icon()
            {
                Base64Data = base64ImageRepresentation,
                MediaType = MediaType
            };
        }

    }
}

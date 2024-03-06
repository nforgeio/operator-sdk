// -----------------------------------------------------------------------------
// FILE:	    Icon.cs
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

using YamlDotNet.Serialization;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// The icon for this operator.
    /// </summary>
    public class Icon
    {
        /// <summary>
        /// base64 data
        /// </summary>
        [YamlMember(Alias = "base64data", ApplyNamingConventions = false)]
        public string Base64Data { get; set; }

        /// <summary>
        /// mediaType
        /// </summary>
        [YamlMember(Alias = "mediatype", ApplyNamingConventions = false)]
        public string MediaType { get; set; }

        /// <summary>
        /// Convert an IconAttribute to an <see cref="Icon"/>.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static Icon FromAttribute(IconAttribute attribute)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(attribute.Path); // read the bytes
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            return new Icon()
            {
                Base64Data = base64ImageRepresentation,
                MediaType = attribute.MediaType
            };
        }
    }
}

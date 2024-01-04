//-----------------------------------------------------------------------------
// FILE:	    OperatorHelper.cs
// CONTRIBUTOR: Jeff Lill, Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

using k8s.Models;

using Microsoft.AspNetCore.JsonPatch;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Neon.Operator.Util
{
    /// <summary>
    /// Useful utilities for the operator SDK.
    /// </summary>
    public static class OperatorHelper
    {
        private static readonly JsonSerializerSettings k8sSerializerSettings;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static OperatorHelper()
        {
            // Create a NewtonSoft JSON serializer with settings compatible with Kubernetes.

            k8sSerializerSettings = new JsonSerializerSettings()
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
                Converters       = new List<JsonConverter>() { new Newtonsoft.Json.Converters.StringEnumConverter() },
                ContractResolver = new CamelCasePropertyNamesContractResolver()

            };
        }

        /// <summary>
        /// Returns the default Newtonsoft contract resolver used for generating CRDs.
        /// This defaults to <see cref="CamelCaseNamingStrategy"/>.
        /// </summary>
        public static DefaultContractResolver DefaultContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        /// <summary>
        /// Creates a new <see cref="JsonPatchDocument"/> that can be used to specify modifications
        /// to a <typeparamref name="T"/> custom object.
        /// </summary>
        /// <typeparam name="T">Specifies the custom object type.</typeparam>
        /// <returns>The <see cref="JsonPatchDocument"/>.</returns>
        public static JsonPatchDocument<T> CreatePatch<T>()
            where T : class
        {
            return new JsonPatchDocument<T>()
            {
                ContractResolver = DefaultContractResolver
            };
        }

        /// <summary>
        /// Converts a <see cref="JsonPatchDocument"/> into a <see cref="V1Patch"/> that
        /// can be submitted to the Kubernetes API.
        /// </summary>
        /// <typeparam name="T">Identifies the type being patched.</typeparam>
        /// <param name="patchDoc">The configured patch document.</param>
        /// <returns>The <see cref="V1Patch"/> instance.</returns>
        public static V1Patch ToV1Patch<T>(JsonPatchDocument<T> patchDoc)
            where T : class
        {
            Covenant.Requires<ArgumentNullException>(patchDoc != null, nameof(patchDoc));

            var patchJson = JsonConvert.SerializeObject(patchDoc, Formatting.None, k8sSerializerSettings);

            return new V1Patch(patchJson, V1Patch.PatchType.JsonPatch);
        }
    }
}

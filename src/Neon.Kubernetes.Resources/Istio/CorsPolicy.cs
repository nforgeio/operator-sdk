//-----------------------------------------------------------------------------
// FILE:        CorsPolicy.cs
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

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using k8s;

using Neon.K8s.Resources.JsonConverters;

namespace Neon.K8s.Resources.Istio
{
    /// <summary>
    /// Describes the Cross-Origin Resource Sharing (CORS) policy, for a given service. Refer to CORS for further details about cross 
    /// origin resource sharing. For example, the following rule restricts cross origin requests to those originating from example.com domain 
    /// using HTTP POST/GET, and sets the Access-Control-Allow-Credentials header to false. In addition, it only exposes X-Foo-bar header and 
    /// sets an expiry period of 1 day.Describes the CorsPolicy V1VirtualService.
    /// </summary>
    public class CorsPolicy : IValidate
    {
        /// <summary>
        /// Initializes a new instance of the CorsPolicy class.
        /// </summary>
        public CorsPolicy()
        {
        }

        /// <summary>
        /// String patterns that match allowed origins. An origin is allowed if any of the string matchers match. If a match is found, then the 
        /// outgoing Access-Control-Allow-Origin would be set to the origin as provided by the client.
        /// </summary>
        [DefaultValue(null)]
        public List<StringMatch> AllowOrigins { get; set; }

        /// <summary>
        /// List of HTTP methods allowed to access the resource. The content will be serialized as the <b>Access-Control-Allow-Methods</b> header.
        /// </summary>
        [DefaultValue(null)]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonCollectionItemConverter<HTTPMethod, System.Text.Json.Serialization.JsonStringEnumMemberConverter>))]
        public IEnumerable<HTTPMethod> AllowMethods { get; set; }

        /// <summary>
        /// List of HTTP headers that can be used when requesting the resource. Serialized as the <b>Access-Control-Allow-Headers</b> header.
        /// </summary>
        [DefaultValue(null)]
        public List<string> AllowHeaders { get; set; }

        /// <summary>
        /// A list of HTTP headers that the browsers are allowed to access. Serialized as the <b>Access-Control-Expose-Headers</b> header.
        /// </summary>
        [DefaultValue(null)]
        public List<string> ExposeHeaders { get; set; }

        /// <summary>
        /// Specifies how long the results of a preflight request can be cached. Serializes as the <b>Access-Control-Max-Age</b> header.
        /// </summary>
        [DefaultValue(null)]
        public string MaxAge { get; set; }

        /// <summary>
        /// Indicates whether the caller is allowed to send the actual request (not the preflight) using credentials. 
        /// Serialized as the <b>Access-Control-Allow-Credentials</b> header.
        /// </summary>
        [DefaultValue(null)]
        public bool? AllowCredentials { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">Thrown if validation fails.</exception>
        public virtual void Validate()
        {
        }
    }
}

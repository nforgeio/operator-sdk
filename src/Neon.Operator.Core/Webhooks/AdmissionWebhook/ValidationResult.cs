//-----------------------------------------------------------------------------
// FILE:	    ValidationResult.cs
// CONTRIBUTOR: Marcus Bowyer
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

using Microsoft.AspNetCore.Http;

namespace Neon.Operator.Webhooks
{
    /// <summary>
    /// Represents the result of a validation webhook.
    /// </summary>
    public sealed class ValidationResult : AdmissionResult
    {
        /// <summary>
        /// Constructs a success response with optional warnings.
        /// </summary>
        /// <param name="warnings">Specifies zero or more warning strings.</param>
        /// <returns>The <see cref="ValidationResult"/>.</returns>
        public static ValidationResult Success(params string[] warnings)
        {
            return new ValidationResult()
            {
                Valid      = true,
                StatusCode = StatusCodes.Status200OK,
                Warnings   = warnings
            };
        }

        /// <summary>
        /// Constructs a fail result with optional status code and status message.
        /// </summary>
        /// <param name="statusCode">Optionally specifies the HTTP status code.</param>
        /// <param name="statusMessage">Optionally specifies the status message.</param>
        /// <returns>The <see cref="ValidationResult"/>.</returns>
        public static ValidationResult Fail(int? statusCode = null, string statusMessage = null)
        {
            return new ValidationResult()
            {
                Valid         = false,
                StatusCode    = statusCode,
                StatusMessage = statusMessage
            };
        }
    }
}
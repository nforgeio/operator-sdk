// -----------------------------------------------------------------------------
// FILE:	    RequeueException.cs
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

using Neon.Operator.Controllers;

namespace Neon.Operator.Core.Exceptions
{
    /// <summary>
    /// Represents an exception that indicates the need to requeue an operation.
    /// </summary>
    public class RequeueException : Exception
    {
        /// <summary>
        /// Gets or sets the delay before requeuing the operation.
        /// </summary>
        public TimeSpan? Delay { get; internal set; }

        /// <summary>
        /// Gets or sets the type of event that triggered the requeue.
        /// </summary>
        public WatchEventType? EventType { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequeueException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="delay">The delay before requeuing the operation.</param>
        /// <param name="eventType">The type of event that triggered the requeue.</param>
        public RequeueException(
            string          message   = null,
            TimeSpan?       delay     = null,
            WatchEventType? eventType = null)
            : base(message)
        {
            this.Delay     = delay;
            this.EventType = eventType;
        }
    }
}

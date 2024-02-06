// -----------------------------------------------------------------------------
// FILE:	    WatchEventTypee.cs
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

using System.Runtime.Serialization;

namespace Neon.Operator.Controllers
{
    /// <summary>
    /// Enumerates kubernetes watch event types.
    /// </summary>
    public enum WatchEventType
    {
        /// <summary>Emitted when an object is created, modified to match a watch's filter, or when a watch is first opened.</summary>
        [EnumMember(Value = "ADDED")]
        Added,

        /// <summary>Emitted when an object is modified.</summary>
        [EnumMember(Value = "MODIFIED")]
        Modified,

        /// <summary>Emitted when an object is deleted or modified to no longer match a watch's filter.</summary>
        [EnumMember(Value = "DELETED")]
        Deleted,

        /// <summary>Emitted when an error occurs while watching resources. Most commonly, the error is 410 Gone which indicates that
        /// the watch resource version was outdated and events were probably lost. In that case, the watch should be restarted.
        /// </summary>
        [EnumMember(Value = "ERROR")]
        Error,

        /// <summary>Bookmarks may be emitted periodically to update the resource version. The object will
        /// contain only the resource version.
        /// </summary>
        [EnumMember(Value = "BOOKMARK")]
        Bookmark,
    }
}

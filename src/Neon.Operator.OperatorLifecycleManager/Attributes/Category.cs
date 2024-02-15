// -----------------------------------------------------------------------------
// FILE:	    Category.cs
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
using System.Runtime.Serialization;

namespace Neon.Operator.OperatorLifecycleManager
{
    /// <summary>
    /// Category of the operator.
    /// </summary>
    [Flags]
    public enum Category
    {

        /// <summarys
        /// AI/Machine Learning
        /// </summary>
        [EnumMember(Value = "AI/Machine Learning")]
        AiMachineLearning = 0,

        /// <summary>
        /// Application Runtime
        /// </summary>
        [EnumMember(Value = "Application Runtime")]
        ApplicationRuntime = 1,

        /// <summary>
        /// Big Data
        /// </summary>
        [EnumMember(Value = "Big Data")]
        BigData = 2,

        /// <summary>
        /// Cloud Provider
        /// </summary>
        [EnumMember(Value = "Cloud Provider")]
        CloudProvider = 4,

        /// <summary>
        /// Database
        /// </summary>
        [EnumMember(Value = "Database")]
        Database = 8,

        /// <summary>
        /// Developer Tools
        /// </summary>
        [EnumMember(Value = "Developer Tools")]
        DeveloperTools = 16,

        /// <summary>
        /// Drivers and plugins
        /// </summary>
        [EnumMember(Value = "Drivers and plugins")]
        DriversAndPlugins = 32,

        /// <summary>
        /// Integration & Delivery
        /// </summary>
        [EnumMember(Value = "Integration & Delivery")]
        IntegrationAndDelivery = 64,

        /// <summary>
        /// Logging & Tracing
        /// </summary>
        [EnumMember(Value = "Logging & Tracing")]
        LoggingAndTracing = 128,

        /// <summary>
        /// Modernization & Migration
        /// </summary>
        [EnumMember(Value ="Modernization & Migration")]
        ModernizationMigration = 256,

        /// <summary>
        /// Monitoring
        /// </summary>
        [EnumMember(Value ="Monitoring")]
        Monitoring = 512,

        /// <summary>
        /// Networking
        /// </summary>
        [EnumMember(Value ="Networking")]
        Networking = 1024,

        /// <summary>
        /// OpenShift Optional
        /// </summary>
        [EnumMember(Value ="OpenShift Optional")]
        OpenShiftOptional = 2048,

        /// <summary>
        /// Security
        /// </summary>
        [EnumMember(Value ="Security")]
        Security = 4096,

        /// <summary>
        /// Storage
        /// </summary>
        [EnumMember(Value ="Storage")]
        Storage = 8192,

        /// <summary>
        /// Streaming & Messaging
        /// </summary>
        [EnumMember(Value ="Streaming & Messaging")]
        StreamingMessaging = 16384,




    }
}

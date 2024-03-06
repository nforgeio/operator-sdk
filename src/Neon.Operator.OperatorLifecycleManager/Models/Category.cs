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

        /// <summary>
        /// AI/Machine Learning
        /// </summary>
        [EnumMember(Value = "AI/Machine Learning")]
        AiMachineLearning = 1,

        /// <summary>
        /// Application Runtime
        /// </summary>
        [EnumMember(Value = "Application Runtime")]
        ApplicationRuntime = 2,

        /// <summary>
        /// Big Data
        /// </summary>
        [EnumMember(Value = "Big Data")]
        BigData = 4,

        /// <summary>
        /// Cloud Provider
        /// </summary>
        [EnumMember(Value = "Cloud Provider")]
        CloudProvider = 8,

        /// <summary>
        /// Database
        /// </summary>
        [EnumMember(Value = "Database")]
        Database = 16,

        /// <summary>
        /// Developer Tools
        /// </summary>
        [EnumMember(Value = "Developer Tools")]
        DeveloperTools = 32,

        /// <summary>
        /// Drivers and plugins
        /// </summary>
        [EnumMember(Value = "Drivers and plugins")]
        DriversAndPlugins = 64,

        /// <summary>
        /// Integration and Delivery
        /// </summary>
        [EnumMember(Value = "Integration & Delivery")]
        IntegrationAndDelivery = 128,

        /// <summary>
        /// Logging and Tracing
        /// </summary>
        [EnumMember(Value = "Logging & Tracing")]
        LoggingAndTracing = 256,

        /// <summary>
        /// Modernization and Migration
        /// </summary>
        [EnumMember(Value ="Modernization & Migration")]
        ModernizationMigration = 512,

        /// <summary>
        /// Monitoring
        /// </summary>
        [EnumMember(Value ="Monitoring")]
        Monitoring = 1024,

        /// <summary>
        /// Networking
        /// </summary>
        [EnumMember(Value ="Networking")]
        Networking = 2048,

        /// <summary>
        /// OpenShift Optional
        /// </summary>
        [EnumMember(Value ="OpenShift Optional")]
        OpenShiftOptional = 4096,

        /// <summary>
        /// Security
        /// </summary>
        [EnumMember(Value ="Security")]
        Security = 8192,

        /// <summary>
        /// Storage
        /// </summary>
        [EnumMember(Value ="Storage")]
        Storage = 16384,

        /// <summary>
        /// Streaming and Messaging
        /// </summary>
        [EnumMember(Value ="Streaming & Messaging")]
        StreamingMessaging = 32768,




    }
}

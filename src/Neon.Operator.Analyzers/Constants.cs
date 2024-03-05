// -----------------------------------------------------------------------------
// FILE:	    Constants.cs
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

using Neon.Operator.Attributes;
using Neon.Operator.OperatorLifecycleManager;
using System.Collections.Generic;

namespace Neon.Operator.Analyzers
{
    internal static class Constants
    {
        public const string AutoGeneratedHeader = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the NeonFORGE Operator SDK.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------";
        public const string ObjectTypeString = "object";
        public const string StringTypeString = "string";
        public const string BooleanTypeString = "boolean";
        public const string IntegerTypeString = "integer";
        public const string NumberTypeString = "number";
        public const string ArrayTypeString = "array";
        public const string Int32TypeString = "int32";
        public const string Int64TypeString = "int64";
        public const string FloatTypeString = "float";
        public const string DoubleTypeString = "double";
        public const string DateTimeTypeString = "date-time";

        public const int DefaultWebhookPort = 5000;

        public static List<string> AssemblyAttributeNames = [
            nameof(AnnotationAttribute),
            nameof(AnnotationAttribute).Replace("Attribute", ""),
            nameof(DescriptionAttribute),
            nameof(DescriptionAttribute).Replace("Attribute", ""),
            nameof(InstallModeAttribute),
            nameof(InstallModeAttribute).Replace("Attribute", ""),
            nameof(MaintainerAttribute),
            nameof(MaintainerAttribute).Replace("Attribute", ""),
            nameof(MaturityAttribute),
            nameof(MaturityAttribute).Replace("Attribute", ""),
            nameof(MinKubeVersionAttribute),
            nameof(MinKubeVersionAttribute).Replace("Attribute", ""),
            nameof(DisplayNameAttribute),
            nameof(DisplayNameAttribute).Replace("Attribute", ""),
            nameof(KeywordAttribute),
            nameof(KeywordAttribute).Replace("Attribute", ""),
            nameof(NameAttribute),
            nameof(NameAttribute).Replace("Attribute", ""),
            nameof(VersionAttribute),
            nameof(VersionAttribute).Replace("Attribute", ""),
            nameof(ProviderAttribute),
            nameof(ProviderAttribute).Replace("Attribute", ""),
            nameof(ContainerImageAttribute),
            nameof(ContainerImageAttribute).Replace("Attribute", ""),
            nameof(IconAttribute),
            nameof(IconAttribute).Replace("Attribute", ""),
            nameof(CapabilitiesAttribute),
            nameof(CapabilitiesAttribute).Replace("Attribute", ""),
            nameof(CategoryAttribute),
            nameof(CategoryAttribute).Replace("Attribute", ""),
            nameof(RepositoryAttribute),
            nameof(RepositoryAttribute).Replace("Attribute", ""),
            nameof(DefaultChannelAttribute),
            nameof(DefaultChannelAttribute).Replace("Attribute", ""),
            nameof(ReviewersAttribute),
            nameof(ReviewersAttribute).Replace("Attribute", ""),
            nameof(UpdateGraphAttribute),
            nameof(UpdateGraphAttribute).Replace("Attribute", ""),
            nameof(LinkAttribute),
            nameof(LinkAttribute).Replace("Attribute", ""),
            ];

        public static class Labels
        {
            public const string Name = "app.kubernetes.io/name";
            public const string Version = "app.kubernetes.io/version";
        }

        public static class Annotations
        {
            public const string PrometheusPath = "prometheus.io/path";
            public const string PrometheusPort = "prometheus.io/port";
            public const string PrometheusScheme = "prometheus.io/scheme";
            public const string PrometheusScrape = "prometheus.io/scrape";
        }
    }
}

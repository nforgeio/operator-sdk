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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neon.Operator.Analyzers
{
    internal static class Constants
    {
        static Constants()
        {
            AssemblyAttributeNames = AssemblyAttributeTypes.SelectMany(a => new string[] { a.Name, a.Name.Replace("Attribute", "") }).ToList();
        }

        public const string AutoGeneratedHeader = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the NeonFORGE Operator SDK.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------";
        public const string YamlCrdHeader = "# This CustomResourceDefinition was generated by the NeonFORGE Operator SDK.";
        public const string YamlCsvHeader = "# This ClusterServiceVersion was generated by the NeonFORGE Operator SDK.";
        public const string YamlExtension = ".yaml";
        public const string GeneratedYamlExtension = ".g.yaml";
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

        public static List<string> AssemblyAttributeNames { get; }

        public static List<Type> AssemblyAttributeTypes = [
            typeof(AnnotationAttribute),
            typeof(CapabilitiesAttribute),
            typeof(CategoryAttribute),
            typeof(CertifiedAttribute),
            typeof(ContainerImageAttribute),
            typeof(DefaultChannelAttribute),
            typeof(DescriptionAttribute),
            typeof(DisplayNameAttribute),
            typeof(IconAttribute),
            typeof(InstallModeAttribute),
            typeof(KeywordAttribute),
            typeof(LabelAttribute),
            typeof(LinkAttribute),
            typeof(MaintainerAttribute),
            typeof(MaturityAttribute),
            typeof(MinKubeVersionAttribute),
            typeof(NameAttribute),
            typeof(OwnedEntityAttribute),
            typeof(OwnedEntityAttribute<>),
            typeof(ProviderAttribute),
            typeof(RepositoryAttribute),
            typeof(RequiredEntityAttribute),
            typeof(RequiredEntityAttribute<>),
            typeof(ReviewersAttribute),
            typeof(UpdateGraphAttribute),
            typeof(VersionAttribute),
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

//-----------------------------------------------------------------------------
// FILE:	    AssemblyInfo.cs
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


using Neon.Operator.Attributes;
using Neon.Operator.OperatorLifecycleManager;

using TestOperator;

[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

[assembly: Name("test-operator")]
[assembly: DisplayName("testaroo operator")]
[assembly: OwnedEntity<V1ExampleEntity>(Description = "This is the description", DisplayName = "Example Operator")]
[assembly: Description(ShortDescription = "this is the short description", FullDescription = Constants.FullDescription)]
[assembly: Provider(Name = "Example", Url = "www.example.com")]
[assembly: Maintainer(Name = "Some Corp", Email = "foo@bar.com")]
[assembly: Version("1.2.3")]
[assembly: Maturity("alpha")]
[assembly: MinKubeVersion("1.16.0")]
[assembly: Keyword("test", "app")]
[assembly: Icon(Path = "nuget-icon.png", MediaType = "image/png")]
[assembly: Repository(Repository = "https://github.com/test-operator/cluster-operator")]
[assembly: Category(Category = Category.DeveloperTools)]
[assembly: Category(Category = Category.ApplicationRuntime)]
[assembly: Capabilities(Capability = CapabilityLevel.DeepInsights)]
[assembly: ContainerImage(Repository = "github.com/test-operator/cluster-operator", Tag ="1.2.3")]
[assembly: InstallMode(Type = InstallModeType.OwnNamespace | InstallModeType.MultiNamespace | InstallModeType.SingleNamespace)]
[assembly: InstallMode(Type = InstallModeType.AllNamespaces, Supported = false)]
[assembly: DefaultChannel("stable")]

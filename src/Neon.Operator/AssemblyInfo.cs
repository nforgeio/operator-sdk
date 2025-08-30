//-----------------------------------------------------------------------------
// FILE:	    AssemblyInfo.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2025 by NEONFORGE LLC.  All rights reserved.
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

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyProduct("OperatorSDK")]
[assembly: AssemblyCompany("NEONFORGE LLC")]
[assembly: AssemblyCopyright("Copyright © 2005-2025 by NEONFORGE LLC.  All rights reserved.")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: InternalsVisibleTo("Neon.Operator")]
[assembly: InternalsVisibleTo("Neon.Operator.XUnit")]
[assembly: InternalsVisibleTo("Neon.Kube.Xunit")]
[assembly: InternalsVisibleTo("Test.Neon.Kube")]
[assembly: InternalsVisibleTo("Test.Neon.Operator")]

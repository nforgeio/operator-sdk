// -----------------------------------------------------------------------------
// FILE:	    CompilationHelper.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit.Abstractions;

namespace Test.Analyzers
{
    internal static class CompilationHelper
    {
        public static string GetGeneratedOutput<T>(
            string source,
            bool executable = false,
            ITestOutputHelper output = null,
            List<Assembly> additionalAssemblies = null)
            where T : ISourceGenerator, new()
        {
            var outputCompilation = CreateCompilation<T>(source, executable);
            var trees = outputCompilation.SyntaxTrees.Reverse().Take(2).Reverse().ToList();

            if (output != null)
            {
                foreach (var tree in trees)
                {
                    output?.WriteLine(Path.GetFileName(tree.FilePath) + ":");
                    output?.WriteLine(tree.ToString());
                }
            }

            return (trees[1].ToString());
        }

        public static Compilation CreateCompilation<T>(
            string         source,
            bool           executable,
            List<Assembly> additionalAssemblies = null)
            where T : ISourceGenerator, new()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));

            if (additionalAssemblies != null)
            {
                foreach (var assembly in additionalAssemblies)
                    if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            var compilation = CSharpCompilation.Create("Foo",
                                                   new SyntaxTree[] { syntaxTree },
                                                   references,
                                                   new CSharpCompilationOptions(executable ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary));

            var generator = new T();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

            return outputCompilation;
        }
    }
}

// -----------------------------------------------------------------------------
// FILE:	    Test_Extensinos.cs
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Operator.Analyzers;
using Neon.Operator.OperatorLifecycleManager;
using Neon.Roslyn;
using Neon.Roslyn.Xunit;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Test.Analyzers
{
    public class Test_Extensions
    {
        [Fact]
        public void TestGetBinaryEnum()
        {
            var source = $@"
using Neon.Operator.OperatorLifecycleManager;

namespace TestNamespace
{{
    public class TestClass
    {{
        public Category Foo = Category.ApplicationRuntime | Category.Database;
    }}
}}";
            var testCompilation = new TestCompilationBuilder()
                .AddSource(source)
                .AddAssembly(typeof(Category).Assembly)
                .Build();

            var context = new MetadataLoadContext(testCompilation.Compilation);

            var syntax = testCompilation.Compilation.SyntaxTrees
                .FirstOrDefault()
                .GetRoot()
                .DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .FirstOrDefault();

            var expected = (int)(Category.ApplicationRuntime | Category.Database);

            syntax.GetEnumValue(context).Should().Be(expected);
        }

        [Fact]
        public void TestGetMultipleBinaryEnum()
        {
            var source = $@"
using Neon.Operator.OperatorLifecycleManager;

namespace TestNamespace
{{
    public class TestClass
    {{
        public Category Foo = Category.ApplicationRuntime | Category.Database | Category.CloudProvider | Category.ModernizationMigration;
    }}
}}";
            var testCompilation = new TestCompilationBuilder()
                .AddSource(source)
                .AddAssembly(typeof(Category).Assembly)
                .Build();

            var context = new MetadataLoadContext(testCompilation.Compilation);

            var syntax = testCompilation.Compilation.SyntaxTrees
                .FirstOrDefault()
                .GetRoot()
                .DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .FirstOrDefault();

            var expected = (int)(Category.ApplicationRuntime | Category.Database | Category.CloudProvider | Category.ModernizationMigration);
            syntax.GetEnumValue(context).Should().Be(expected);
        }
    }
}

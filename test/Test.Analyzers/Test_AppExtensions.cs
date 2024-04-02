// -----------------------------------------------------------------------------
// FILE:	    TestAppExtensions.cs
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

using System.Threading.Tasks;

using FluentAssertions;

using k8s.Models;

using Neon.Operator.Analyzers;

using Neon.Operator.Attributes;

using Neon.Roslyn.Xunit;
using Neon.Xunit;

namespace Test.Analyzers
{
    [Trait(TestTrait.Category, TestArea.NeonOperator)]
    public class Test_AppExtensions
    {
        [Fact]
        public async Task TestUseKubernetesOperator()
        {
            await Task.CompletedTask;

            var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neon.Operator;

namespace TestNamespace
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddConsole();
            });

            services.AddKubernetesOperator();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseKubernetesOperator();
        }
    }
}";
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<AppExtensionsGenerator>()
                .AddSource(source)
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .Build();

            testCompilation.Sources.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestDontUseKubernetesOperator()
        {
            await Task.CompletedTask;

            var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neon.Operator;

namespace TestNamespace
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddConsole();
            });

            services.AddKubernetesOperator();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}";
            var testCompilation = new TestCompilationBuilder()
                .AddSourceGenerator<AppExtensionsGenerator>()
                .AddSource(source)
                .AddAssembly(typeof(KubernetesEntityAttribute).Assembly)
                .AddAssembly(typeof(AdditionalPrinterColumnAttribute).Assembly)
                .Build();

            testCompilation.Sources.Should().BeEmpty();
        }
    }
}

// FILE:	    TestApiServerStartup.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using k8s;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Neon.Operator.Xunit
{
    /// <summary>
    /// Startup class for the test API server.
    /// </summary>
    public class TestApiServerStartup
    {
        /// <summary>
        /// Configures depdendency injection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            Covenant.Requires<ArgumentNullException>(services != null, nameof(services));

            var kubernetesJsonType = typeof(KubernetesJson).Assembly.GetType("k8s.KubernetesJson");

            RuntimeHelpers.RunClassConstructor(kubernetesJsonType.TypeHandle);

            var member  = kubernetesJsonType.GetField("JsonSerializerOptions", BindingFlags.Static | BindingFlags.NonPublic);
            var jsonOptions = (JsonSerializerOptions)member.GetValue(kubernetesJsonType);

            services.AddSingleton(jsonOptions);

            services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, JPIF.GetJsonPatchInputFormatter());
            })
                .AddJsonOptions(
                    options => 
                    {
                        options.JsonSerializerOptions.DictionaryKeyPolicy  = JsonNamingPolicy.CamelCase;
                        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        foreach (var converter in jsonOptions.Converters)
                        {
                            options.JsonSerializerOptions.Converters.Add(converter);
                        }
                    });

            services.AddSingleton<ITestApiServer, TestApiServer>();
        }

        /// <summary>
        /// Configures operator service controllers.
        /// </summary>
        /// <param name="app">Specifies the application builder.</param>
        /// <param name="apiServer">Specifies the test API server.</param>
        public void Configure(IApplicationBuilder app, ITestApiServer apiServer)
        {
            Covenant.Requires<ArgumentNullException>(app != null, nameof(app));
            Covenant.Requires<ArgumentNullException>(apiServer != null, nameof(apiServer));

            app.Use(next => async context =>
            {
                // This is a no-op, but very convenient for setting a breakpoint to see per-request details.
                await next(context);
            });

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.Run(apiServer.UnhandledRequest);
        }
    }

    /// <summary>
    /// Used for getting <see cref="JsonPatchDocument"/> Formatter.
    /// </summary>
    public static class JPIF
    {
        /// <summary>
        /// Gets a <see cref="JsonPatchDocument"/> formatter.
        /// </summary>
        /// <returns></returns>
        public static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
        {
            var builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson()
            .Services.BuildServiceProvider();

            return builder
                .GetRequiredService<IOptions<MvcOptions>>()
                .Value
                .InputFormatters
                .OfType<NewtonsoftJsonPatchInputFormatter>()
                .First();
        }
    }
}

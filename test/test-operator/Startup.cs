using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestOperator
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
            services.AddLogging(c =>
            {
                c.ClearProviders();
                c.AddConsole();
            });

            //services.AddKubernetesOperator();
        }

        public void Configure(IApplicationBuilder app)
        {
            //app.UseKubernetesOperator();
        }
    }
}

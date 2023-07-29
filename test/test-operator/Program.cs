using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;

using Neon.Operator;

namespace TestOperator
{
    public static partial class Program
    {
        public static async Task Main(string[] args)
        {
            var k8s = KubernetesOperatorHost
               .CreateDefaultBuilder()
               .ConfigureOperator(configure =>
               {
                   configure.AssemblyScanningEnabled = false;
                   configure.DeployedNamespace       = "default";
               })
               .ConfigureNeonKube()
               .UseStartup<Startup>().Build();

            await k8s.RunAsync();
        }
    }
}
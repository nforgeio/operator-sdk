using System.Threading.Tasks;

using k8s;
using k8s.Models ;

using Microsoft.AspNetCore.Hosting;

using Neon.Operator;
using Neon.Operator.Attributes;
using Neon.Operator.Rbac;

namespace TestOperator
{
    [RbacRule<V1ConfigMap>(Verbs = RbacVerb.All, Scope = EntityScope.Cluster)]
    [RbacRule<V1Secret>(Verbs = RbacVerb.All, Scope = EntityScope.Cluster)]
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
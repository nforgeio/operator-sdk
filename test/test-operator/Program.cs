using System.Threading.Tasks;

using k8s.Models;

using Microsoft.AspNetCore.Hosting;

using Neon.Operator;
using Neon.Operator.Attributes;
using Neon.Operator.Rbac;

namespace TestOperator
{
    [RbacRule<V1ConfigMap>(Verbs = RbacVerb.All, Scope = EntityScope.Cluster)]
    [RbacRule<V1Secret>(Verbs = RbacVerb.All, Scope = EntityScope.Cluster)]
    [RbacRule<V1Service>(Verbs = Neon.Operator.Rbac.RbacVerb.Watch)]
    [RbacRule<V1Pod>(Verbs = Neon.Operator.Rbac.RbacVerb.Watch)]
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var k8s = KubernetesOperatorHost
               .CreateDefaultBuilder()
               .ConfigureOperator(configure =>
               {
                   configure.AssemblyScanningEnabled = false;
                   configure.PodNamespace            = "default";
               })
               .ConfigureNeonKube()
               .UseStartup<Startup>().Build();

            await k8s.RunAsync();
        }
    }
}
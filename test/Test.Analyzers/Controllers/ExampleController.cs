using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Logging;

using Neon.Operator.Attributes;
using Neon.Operator.Controllers;
using Neon.Operator.Finalizers;
using Neon.Tasks;

namespace Test_Analyzers
{
    [RbacRule<V1ExampleEntity>(Verbs = Neon.Operator.Rbac.RbacVerb.All)]
    [RbacRule<V2ExampleEntity>(Verbs = Neon.Operator.Rbac.RbacVerb.Watch)]
    [RbacRule(ApiGroup = "foo.testaroo.io", Resource = "foos", Verbs = Neon.Operator.Rbac.RbacVerb.Watch, SubResources = "status,foo", ResourceNames = "bar")]
    [ResourceController()]
    public class ExampleController : ResourceControllerBase<V1ExampleEntity>
    {
        private readonly IKubernetes                        k8s;
        private readonly IFinalizerManager<V1ExampleEntity> finalizerManager;
        private readonly ILogger<ExampleController>         logger;

        public ExampleController(
            IKubernetes                        k8s,
            IFinalizerManager<V1ExampleEntity> finalizerManager,
            ILogger<ExampleController>         logger)
        {
            this.k8s              = k8s;
            this.finalizerManager = finalizerManager;
            this.logger           = logger;
        }

        public override async Task<ResourceControllerResult> ReconcileAsync(V1ExampleEntity resource)
        {
            await SyncContext.Clear;

            logger.LogInformation($"RECONCILING: {resource.Name()}");

            await finalizerManager.RegisterAllFinalizersAsync(resource);

            logger.LogInformation($"RECONCILED: {resource.Name()}");

            return Ok();
        }
    }
}

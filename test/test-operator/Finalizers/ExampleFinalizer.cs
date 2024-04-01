using System.Threading;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Logging;

using Neon.Operator.Attributes;
using Neon.Operator.Finalizers;
using Neon.Tasks;

namespace TestOperator
{
    [RbacRule<V1ExampleEntity>(Verbs = Neon.Operator.Rbac.RbacVerb.All)]
    public class ExampleFinalizer : ResourceFinalizerBase<V1ExampleEntity>
    {
        private readonly IKubernetes k8s;
        private readonly ILogger<ExampleController> logger;
        public ExampleFinalizer(
            IKubernetes k8s,
            ILogger<ExampleController> logger)
        {
            this.k8s = k8s;
            this.logger = logger;
        }

        public override async Task FinalizeAsync(V1ExampleEntity resource, CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            logger.LogInformation($"FINALIZED: {resource.Name()}");
        }
    }
}

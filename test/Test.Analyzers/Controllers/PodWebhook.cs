using System.Threading.Tasks;

using k8s.Models;

using Neon.Operator.Attributes;
using Neon.Operator.Webhooks;

namespace Test_Analyzers
{
    [Webhook(
        name: "pod-mutating-hook.neonkube.io",
        admissionReviewVersions: "v1",
        failurePolicy: "Ignore")]
    [WebhookRule(
        apiGroups: V1Pod.KubeGroup,
        apiVersions: V1Pod.KubeApiVersion,
        operations: AdmissionOperations.Create | AdmissionOperations.Update,
        resources: V1Pod.KubePluralName,
        scope: "*")]
    public class TestarooWebhook : MutatingWebhookBase<V1ExampleEntity>
    {
        private bool modified = false;

        public override async Task<MutationResult> CreateAsync(V1ExampleEntity entity, bool dryRun)
        {
            if (modified)
            {
                return await Task.FromResult(MutationResult.Modified(entity));
            }

            return await Task.FromResult(MutationResult.NoChanges());
        }

        public override async Task<MutationResult> UpdateAsync(V1ExampleEntity entity, V1ExampleEntity oldEntity, bool dryRun)
        {
            if (modified)
            {
                return await Task.FromResult(MutationResult.Modified(entity));
            }

            return await Task.FromResult(MutationResult.NoChanges());
        }
    }
}


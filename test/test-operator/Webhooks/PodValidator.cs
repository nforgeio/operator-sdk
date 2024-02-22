using System.Threading.Tasks;

using k8s.Models;

using Neon.Operator.Webhooks;

namespace TestOperator
{
    [Webhook(
        name: "example-validating-hook.neonkube.io",
        admissionReviewVersions: "v1",
        failurePolicy: FailurePolicy.Ignore)]
    [WebhookRule(
        apiGroups: V1Deployment.KubeGroup,
        apiVersions: V1Deployment.KubeApiVersion,
        operations: AdmissionOperations.Create | AdmissionOperations.Update,
        resources: V1Deployment.KubePluralName,
        scope: "*")]
    public class PodValidator : ValidatingWebhookBase<V1ExampleEntity>
    {
        public override async Task<ValidationResult> CreateAsync(V1ExampleEntity entity, bool dryRun)
        {
            return await Task.FromResult(ValidationResult.Success());
        }

        public override async Task<ValidationResult> UpdateAsync(V1ExampleEntity entity, V1ExampleEntity oldEntity, bool dryRun)
        {
            return await Task.FromResult(ValidationResult.Success());
        }
    }
}


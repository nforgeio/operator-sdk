using System.Threading.Tasks;

using Neon.Operator.Webhooks;

namespace TestOperator
{
    [Webhook(
        name: "testaroo-hook.neonkube.io",
        admissionReviewVersions: "v1",
        failurePolicy: "Ignore")]
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


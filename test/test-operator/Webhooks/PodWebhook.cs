using System.Threading.Tasks;

using Neon.Operator.Webhooks;

namespace test_operator
{
    [Webhook(
        name: "testaroo-hook.neonkube.io",
        admissionReviewVersions: "v1",
        failurePolicy: "Ignore")]
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


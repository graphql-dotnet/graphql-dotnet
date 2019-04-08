using System;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Subscription
{
    [Obsolete("The DocumentExecutor can be used directly to execute subscriptions")]
    public class SubscriptionExecutor : DocumentExecutor, ISubscriptionExecutor
    {
        public SubscriptionExecutor()
        {
        }

        public SubscriptionExecutor(
            IDocumentBuilder documentBuilder,
            IDocumentValidator documentValidator,
            IComplexityAnalyzer complexityAnalyzer)
            : base(documentBuilder, documentValidator, complexityAnalyzer)
        {
        }

        public async Task<SubscriptionExecutionResult> SubscribeAsync(ExecutionOptions config)
        {
            var result = await ExecuteAsync(config).ConfigureAwait(false);

            if (result is SubscriptionExecutionResult subscriptionResult)
            {
                return subscriptionResult;
            }
            else
            {
                return new SubscriptionExecutionResult(result);
            }
        }
    }
}

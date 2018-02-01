using System;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Subscription
{
    [Obsolete("The DocumentExecuter can be used directly to execute subscriptions")]
    public class SubscriptionExecuter : DocumentExecuter, ISubscriptionExecuter
    {
        public SubscriptionExecuter()
        {
        }

        public SubscriptionExecuter(
            IDocumentBuilder documentBuilder, 
            IDocumentValidator documentValidator, 
            IComplexityAnalyzer complexityAnalyzer)
            : base(documentBuilder, documentValidator, complexityAnalyzer)
        {
        }

        public async Task<SubscriptionExecutionResult> SubscribeAsync(ExecutionOptions config)
        {
            return await ExecuteAsync(config).ConfigureAwait(false) as SubscriptionExecutionResult;
        }
    }
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.AST;

namespace GraphQL
{
    [Obsolete("Please use the AddSubscriptionExecutionStrategy() builder method.")]
    public class SubscriptionDocumentExecuter : DocumentExecuter
    {
        public SubscriptionDocumentExecuter()
        {
        }

        public SubscriptionDocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer)
            : base(documentBuilder, documentValidator, complexityAnalyzer)
        {
        }

        public SubscriptionDocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache)
            : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache)
        {
        }

        public SubscriptionDocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache, IEnumerable<IConfigureExecutionOptions> configurations)
            : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache, configurations)
        {
        }

        public SubscriptionDocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer, IDocumentCache documentCache, IEnumerable<IConfigureExecutionOptions> configurations, IExecutionStrategySelector executionStrategySelector)
            : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache, configurations, executionStrategySelector)
        {
        }

        protected override IExecutionStrategy SelectExecutionStrategy(Execution.ExecutionContext context)
        {
            return context.Operation.Operation switch
            {
                OperationType.Subscription => SubscriptionExecutionStrategy.Instance,
                _ => base.SelectExecutionStrategy(context)
            };
        }
    }
}

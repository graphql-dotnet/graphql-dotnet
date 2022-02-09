using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.AST;

namespace GraphQL
{
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

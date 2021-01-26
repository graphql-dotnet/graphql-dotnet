using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

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

        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            return context.Operation.OperationType switch
            {
                OperationType.Subscription => SubscriptionExecutionStrategy.Instance,
                _ => base.SelectExecutionStrategy(context)
            };
        }
    }
}

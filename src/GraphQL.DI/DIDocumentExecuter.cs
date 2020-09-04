using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    //DIDocumentExecuter and DIExecutionStrategy are designed to be registered as scoped service providers
    public class DIDocumentExecuter : DocumentExecuter
    {
        protected DIExecutionStrategy _diExecutionStrategy;
        protected SubscriptionExecutionStrategy _subscriptionExecutionStrategy;

        //pull IDocumentBuilder, IDocumentValidator, IComplexityAnalyzer, DIExecutionStrategy, and SubscriptionExecutionStrategy from DI if they have been registered
        //if any of them have not been registered, use default implementations
        public DIDocumentExecuter(
            IServiceProvider serviceProvider) : base(
                (IDocumentBuilder)(serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider))).GetService(typeof(IDocumentBuilder)) ?? new GraphQLDocumentBuilder(),
                (IDocumentValidator)serviceProvider.GetService(typeof(IDocumentValidator)) ?? new DocumentValidator(),
                (IComplexityAnalyzer)serviceProvider.GetService(typeof(IComplexityAnalyzer)) ?? new ComplexityAnalyzer())
        {
            _diExecutionStrategy = (DIExecutionStrategy)serviceProvider.GetService(typeof(DIExecutionStrategy)) ?? new DIExecutionStrategy();
            _subscriptionExecutionStrategy = (SubscriptionExecutionStrategy)serviceProvider.GetService(typeof(SubscriptionExecutionStrategy)) ?? new SubscriptionExecutionStrategy();
        }

        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            switch (context.Operation.OperationType)
            {
                case OperationType.Query:
                    return _diExecutionStrategy;

                case OperationType.Mutation:
                    return _diExecutionStrategy;

                case OperationType.Subscription:
                    return _subscriptionExecutionStrategy;

                default:
                    throw new InvalidOperationException($"Unexpected OperationType {context.Operation.OperationType}");
            }
        }

    }
}

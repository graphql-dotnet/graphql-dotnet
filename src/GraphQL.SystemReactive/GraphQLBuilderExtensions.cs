using GraphQL.DI;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace GraphQL.SystemReactive
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers <see cref="SubscriptionDocumentExecuter"/> as a singleton of type
        /// <see cref="IDocumentExecuter"/> within the dependency injection framework.
        /// </summary>
        [Obsolete("Please use AddSubscriptionExecutionStrategy()")]
        public static IGraphQLBuilder AddSubscriptionDocumentExecuter(this IGraphQLBuilder builder)
            => builder.AddDocumentExecuter<SubscriptionDocumentExecuter>();

        /// <summary>
        /// Registers <see cref="SubscriptionExecutionStrategy"/> with the dependency engine framework as a
        /// singleton, and registers an <see cref="ExecutionStrategyRegistration"/> for the <see cref="OperationType.Subscription"/>
        /// operation type to use the <see cref="SubscriptionExecutionStrategy"/>.
        /// </summary>
        public static IGraphQLBuilder AddSubscriptionExecutionStrategy(this IGraphQLBuilder builder)
            => builder.AddExecutionStrategy<SubscriptionExecutionStrategy>(OperationType.Subscription);
    }
}

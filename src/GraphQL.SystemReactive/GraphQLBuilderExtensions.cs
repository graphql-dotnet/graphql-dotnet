using GraphQL.DI;

namespace GraphQL.SystemReactive
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers <see cref="SubscriptionDocumentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddSubscriptionDocumentExecuter(this IGraphQLBuilder builder)
            => builder.AddDocumentExecuter<SubscriptionDocumentExecuter>();
    }
}

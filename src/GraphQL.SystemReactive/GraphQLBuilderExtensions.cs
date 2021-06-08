namespace GraphQL.SystemReactive
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddSubscriptionDocumentExecuter(this IGraphQLBuilder builder)
        {
            builder.AddDocumentExecuter<SubscriptionDocumentExecuter>();
            return builder;
        }
    }
}

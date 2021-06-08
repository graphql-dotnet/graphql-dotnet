using GraphQL.DI;

namespace GraphQL.SystemReactive
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddSystemReactive(this IGraphQLBuilder builder)
        {
            builder.Register<SubscriptionDocumentExecuter>(ServiceLifetime.Singleton);
            builder.Register<IDefaultService<IDocumentExecuter>, DefaultService<SubscriptionDocumentExecuter>>(ServiceLifetime.Singleton);
            return builder;
        }
    }
}

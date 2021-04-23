using GraphQL.Execution;

namespace GraphQL.DataLoader
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddDataLoader(this IGraphQLBuilder builder)
        {
            builder.Register<IDocumentExecutionListener, DataLoaderDocumentListener>(ServiceLifetime.Singleton);
            builder.Register<IDataLoaderContextAccessor, DataLoaderContextAccessor>(ServiceLifetime.Singleton);
            return builder;
        }
    }
}

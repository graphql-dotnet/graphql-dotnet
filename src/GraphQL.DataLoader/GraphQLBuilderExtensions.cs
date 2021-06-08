namespace GraphQL.DataLoader
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddDataLoader(this IGraphQLBuilder builder)
        {
            builder.AddDocumentListener<DataLoaderDocumentListener>();
            builder.Register<IDataLoaderContextAccessor, DataLoaderContextAccessor>(ServiceLifetime.Singleton);
            return builder;
        }
    }
}

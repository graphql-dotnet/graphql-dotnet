namespace GraphQL.SystemTextJson
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddDocumentWriter(this IGraphQLBuilder builder)
        {
            builder.Register<IDocumentWriter, DocumentWriter>(ServiceLifetime.Singleton);
            return builder;
        }
    }
}

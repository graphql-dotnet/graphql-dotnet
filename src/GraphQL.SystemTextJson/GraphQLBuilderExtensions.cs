namespace GraphQL.SystemTextJson
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            return builder;
        }
    }
}

namespace GraphQL.NewtonsoftJson
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            return builder;
        }
    }
}

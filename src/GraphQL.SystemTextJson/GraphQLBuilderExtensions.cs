using GraphQL.DI;

namespace GraphQL.SystemTextJson
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder)
        {
            builder.Register<DocumentWriter>(ServiceLifetime.Singleton);
            builder.Register<IDefaultService<IDocumentWriter>, DefaultService<DocumentWriter>>(ServiceLifetime.Singleton);
            return builder;
        }
    }
}

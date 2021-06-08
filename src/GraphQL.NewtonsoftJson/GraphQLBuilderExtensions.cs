using GraphQL.DI;

namespace GraphQL.NewtonsoftJson
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder)
        {
            builder.Register<DocumentWriter>(ServiceLifetime.Singleton);
            builder.Register<IDefaultService<IDocumentWriter>, DefaultService<DocumentWriter>>(ServiceLifetime.Singleton);
            return builder;
        }
    }
}

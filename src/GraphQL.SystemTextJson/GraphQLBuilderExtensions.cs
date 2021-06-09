using System;
using System.Text.Json;

namespace GraphQL.SystemTextJson
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions> action = null)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.Configure(action);
            return builder;
        }

        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions, IServiceProvider> action)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.Configure(action);
            return builder;
        }
    }
}

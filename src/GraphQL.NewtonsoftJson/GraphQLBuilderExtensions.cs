using System;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings> action = null)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.Configure(action);
            return builder;
        }

        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings, IServiceProvider> action)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.Configure(action);
            return builder;
        }
    }
}

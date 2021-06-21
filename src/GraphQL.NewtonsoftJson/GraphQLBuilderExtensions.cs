using System;
using GraphQL.DI;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers Newtonsoft.Json <see cref="DocumentWriter"/> as a singleton of type <see cref="IDocumentWriter"/> within the
        /// dependency injection framework and configures it with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings> action = null)
            => builder.AddDocumentWriter<DocumentWriter>().Configure(action);

        /// <inheritdoc cref="AddNewtonsoftJson(IGraphQLBuilder, Action{JsonSerializerSettings})"/>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings, IServiceProvider> action)
            => builder.AddDocumentWriter<DocumentWriter>().Configure(action);
    }
}

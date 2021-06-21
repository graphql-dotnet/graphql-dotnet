using System;
using System.Text.Json;
using GraphQL.DI;

namespace GraphQL.SystemTextJson
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers the System.Text.Json <see cref="DocumentWriter"/> as a singleton of type <see cref="IDocumentWriter"/> within the
        /// dependency injection framework and configures it with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions> action = null)
            => builder.AddDocumentWriter<DocumentWriter>().Configure(action);

        /// <inheritdoc cref="AddSystemTextJson(IGraphQLBuilder, Action{JsonSerializerOptions})"/>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions, IServiceProvider> action)
            => builder.AddDocumentWriter<DocumentWriter>().Configure(action);
    }
}

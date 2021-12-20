#nullable enable

using System;
using System.Text.Json;
using GraphQL.DI;

namespace GraphQL.SystemTextJson
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers the System.Text.Json <see cref="DocumentWriter"/> and <see cref="GraphQLRequestReader"/> as singletons of types
        /// <see cref="IDocumentWriter"/> and <see cref="IGraphQLRequestReader"/> within the dependency injection framework and configures
        /// them with the specified configuration delegate(s).
        /// </summary>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions>? action = null)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.AddGraphQLRequestReader<GraphQLRequestReader>();
            builder.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddSystemTextJson(IGraphQLBuilder, Action{JsonSerializerOptions})"/>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions, IServiceProvider>? action)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.AddGraphQLRequestReader<GraphQLRequestReader>();
            builder.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddSystemTextJson(IGraphQLBuilder, Action{JsonSerializerOptions})"/>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions> configureReader, Action<JsonSerializerOptions> configureWriter)
        {
            builder.AddDocumentWriter(new DocumentWriter(configureWriter));
            builder.AddGraphQLRequestReader(new GraphQLRequestReader(configureReader));
            return builder;
        }
    }
}

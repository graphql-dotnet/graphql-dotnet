#nullable enable

using System;
using GraphQL.DI;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers the Newtonsoft.Json <see cref="DocumentWriter"/> as a singleton of type
        /// <see cref="IDocumentWriter"/> within the dependency injection framework and configures
        /// it with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings>? action = null)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.AddGraphQLRequestReader<GraphQLRequestReader>();
            builder.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddNewtonsoftJson(IGraphQLBuilder, Action{JsonSerializerSettings})"/>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings, IServiceProvider>? action)
        {
            builder.AddDocumentWriter<DocumentWriter>();
            builder.AddGraphQLRequestReader<GraphQLRequestReader>();
            builder.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddNewtonsoftJson(IGraphQLBuilder, Action{JsonSerializerSettings})"/>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings> configureReader, Action<JsonSerializerSettings> configureWriter)
        {
            builder.AddDocumentWriter(new DocumentWriter(configureWriter));
            builder.AddGraphQLRequestReader(new GraphQLRequestReader(configureReader));
            return builder;
        }
    }
}

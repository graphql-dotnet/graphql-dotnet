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
        /// Registers the System.Text.Json <see cref="GraphQLSerializer"/> as singletons of types
        /// <see cref="IGraphQLSerializer"/> and <see cref="IGraphQLTextSerializer"/> within the dependency
        /// injection framework and configures them with the specified configuration delegate(s).
        /// </summary>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions>? action = null)
        {
            builder.AddSerializer<GraphQLSerializer>();
            builder.Services.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddSystemTextJson(IGraphQLBuilder, Action{JsonSerializerOptions})"/>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions, IServiceProvider>? action)
        {
            builder.AddSerializer<GraphQLSerializer>();
            builder.Services.Configure(action);
            return builder;
        }
    }
}

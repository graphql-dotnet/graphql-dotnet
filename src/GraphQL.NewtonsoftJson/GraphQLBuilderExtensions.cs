#nullable enable

using GraphQL.DI;

namespace GraphQL.NewtonsoftJson
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers the Newtonsoft.Json <see cref="GraphQLSerializer"/> as singletons of types
        /// <see cref="IGraphQLSerializer"/> and <see cref="IGraphQLTextSerializer"/> within the
        /// dependency injection framework and configures it with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings>? action = null)
        {
            builder.Services.Configure(action);
            return builder.AddSerializer<GraphQLSerializer>();
        }

        /// <inheritdoc cref="AddNewtonsoftJson(IGraphQLBuilder, Action{JsonSerializerSettings})"/>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder, Action<JsonSerializerSettings, IServiceProvider>? action)
        {
            builder.Services.Configure(action);
            return builder.AddSerializer<GraphQLSerializer>();
        }
    }
}

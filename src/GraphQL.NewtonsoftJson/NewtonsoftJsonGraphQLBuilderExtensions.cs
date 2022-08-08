#nullable enable

using GraphQL.DI;
using GraphQL.NewtonsoftJson;

namespace GraphQL;

/// <inheritdoc cref="GraphQLBuilderExtensions"/>
public static class NewtonsoftJsonGraphQLBuilderExtensions
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

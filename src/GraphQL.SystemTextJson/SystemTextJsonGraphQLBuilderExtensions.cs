using System.Text.Json;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
#endif
using GraphQL.DI;
using GraphQL.SystemTextJson;

namespace GraphQL;

/// <inheritdoc cref="GraphQLBuilderExtensions"/>
public static class SystemTextJsonGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers the System.Text.Json <see cref="GraphQLSerializer"/> as singletons of types
    /// <see cref="IGraphQLSerializer"/> and <see cref="IGraphQLTextSerializer"/> within the dependency
    /// injection framework and configures them with the specified configuration delegate.
    /// </summary>
    [RequiresUnreferencedCode("This method requires dynamic access to code that is not referenced statically.")]
    [RequiresDynamicCode("Requires runtime code generation for serialization.")]
    public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions>? action = null)
    {
        builder.Services.Configure(action);
        return builder.AddSerializer<GraphQLSerializer>();
    }

    /// <inheritdoc cref="AddSystemTextJson(IGraphQLBuilder, Action{JsonSerializerOptions})"/>
    [RequiresUnreferencedCode("This method requires dynamic access to code that is not referenced statically.")]
    [RequiresDynamicCode("Requires runtime code generation for serialization.")]
    public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions, IServiceProvider>? action)
    {
        builder.Services.Configure(action);
        return builder.AddSerializer<GraphQLSerializer>();
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Registers the System.Text.Json <see cref="GraphQLAotSerializer"/> as singletons of types
    /// <see cref="IGraphQLSerializer"/> and <see cref="IGraphQLTextSerializer"/> within the dependency
    /// injection framework.
    /// </summary>
    public static IGraphQLBuilder AddSystemTextJsonAot(this IGraphQLBuilder builder)
    {
        return builder.AddSerializer<GraphQLAotSerializer>();
    }

    /// <summary>
    /// Registers the System.Text.Json <see cref="GraphQLAotSerializer"/> as singletons of types
    /// <see cref="IGraphQLSerializer"/> and <see cref="IGraphQLTextSerializer"/> within the dependency
    /// injection framework and configures them with the specified <see cref="JsonSerializerContext"/>.
    /// </summary>
    public static IGraphQLBuilder AddSystemTextJsonAot(this IGraphQLBuilder builder, JsonSerializerContext context)
    {
        builder.Services.Register(context);
        return builder.AddSerializer<GraphQLAotSerializer>();
    }
#endif
}

#nullable enable

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json;
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
#if NET5_0_OR_GREATER
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(JsonSerializerOptions))]
#endif
    public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions>? action = null)
    {
        builder.Services.Configure(action);
        return builder.AddSerializer<GraphQLSerializer>();
    }

    /// <inheritdoc cref="AddSystemTextJson(IGraphQLBuilder, Action{JsonSerializerOptions})"/>
#if NET5_0_OR_GREATER
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(JsonSerializerOptions))]
#endif
    public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder, Action<JsonSerializerOptions, IServiceProvider>? action)
    {
        builder.Services.Configure(action);
        return builder.AddSerializer<GraphQLSerializer>();
    }
}

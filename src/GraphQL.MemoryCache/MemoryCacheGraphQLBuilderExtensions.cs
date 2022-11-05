using GraphQL.Caching;
using GraphQL.DI;

namespace GraphQL;

/// <inheritdoc cref="GraphQLBuilderExtensions"/>
public static class MemoryCacheGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="MemoryDocumentCache"/> as a singleton of type <see cref="IConfigureExecution"/> within
    /// the dependency injection framework, and configures it with the specified configuration delegate.
    /// </summary>
    [Obsolete("Please use UseMemoryCache. This method will be removed in v8.")]
    public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions>? action = null)
        => builder.AddMemoryCache(action == null ? null : (options, _) => action(options));

    /// <inheritdoc cref="AddMemoryCache(IGraphQLBuilder, Action{MemoryDocumentCacheOptions})"/>
    [Obsolete("Please use UseMemoryCache. This method will be removed in v8.")]
    public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions, IServiceProvider>? action)
    {
        builder.Services
            .Configure(action)
            .TryRegister<IConfigureExecution, MemoryDocumentCache>(ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);
        return builder;
    }

    /// <summary>
    /// Registers <see cref="MemoryDocumentCache"/> as a singleton of type <see cref="IConfigureExecution"/> within
    /// the dependency injection framework, and configures it with the specified configuration delegate.
    /// </summary>
    public static IGraphQLBuilder UseMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions>? action = null)
     => builder.UseMemoryCache(action == null ? null : (options, _) => action(options));

    /// <inheritdoc cref="AddMemoryCache(IGraphQLBuilder, Action{MemoryDocumentCacheOptions})"/>
    public static IGraphQLBuilder UseMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions, IServiceProvider>? action)
    {
        builder.Services.Configure(action);
        return builder.ConfigureExecution<MemoryDocumentCache>();
    }

    /// <inheritdoc cref="UseAutomaticPersistedQueries(IGraphQLBuilder, Action{AutomaticPersistedQueriesCacheOptions}?)"/>
    [Obsolete("Please use UseAutomaticPersistedQueries. This method will be removed in v8.")]
    public static IGraphQLBuilder AddAutomaticPersistedQueries(this IGraphQLBuilder builder, Action<AutomaticPersistedQueriesCacheOptions>? action = null)
        => UseAutomaticPersistedQueries(builder, action);

    /// <inheritdoc cref="UseAutomaticPersistedQueries(IGraphQLBuilder, Action{AutomaticPersistedQueriesCacheOptions, IServiceProvider}?)"/>
    [Obsolete("Please use UseAutomaticPersistedQueries. This method will be removed in v8.")]
    public static IGraphQLBuilder AddAutomaticPersistedQueries(this IGraphQLBuilder builder, Action<AutomaticPersistedQueriesCacheOptions, IServiceProvider>? action)
        => UseAutomaticPersistedQueries(builder, action);

    /// <summary>
    /// Adds support of Automatic Persisted Queries in the form of implementation of <see cref="IConfigureExecution"/>.
    /// https://www.apollographql.com/docs/react/api/link/persisted-queries/
    /// </summary>
    public static IGraphQLBuilder UseAutomaticPersistedQueries(this IGraphQLBuilder builder, Action<AutomaticPersistedQueriesCacheOptions>? action = null)
        => builder.UseAutomaticPersistedQueries(action == null ? null : (options, _) => action(options));

    /// <inheritdoc cref="UseAutomaticPersistedQueries(IGraphQLBuilder, Action{AutomaticPersistedQueriesCacheOptions})"/>
    public static IGraphQLBuilder UseAutomaticPersistedQueries(this IGraphQLBuilder builder, Action<AutomaticPersistedQueriesCacheOptions, IServiceProvider>? action)
    {
        builder.Services.Configure(action);
        return builder.ConfigureExecution<AutomaticPersistedQueriesExecution>();
    }
}

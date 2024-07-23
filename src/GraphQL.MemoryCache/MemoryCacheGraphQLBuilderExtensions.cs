using GraphQL.Caching;
using GraphQL.DI;

namespace GraphQL;

/// <inheritdoc cref="GraphQLBuilderExtensions"/>
public static class MemoryCacheGraphQLBuilderExtensions
{
    /// <summary>
    /// Caches parsed and validated documents in memory. This is useful for reducing the overhead of parsing
    /// and validating the same document multiple times.
    /// </summary>
    public static IGraphQLBuilder UseMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions>? action = null)
     => builder.UseMemoryCache(action == null ? null : (options, _) => action(options));

    /// <inheritdoc cref="UseMemoryCache(IGraphQLBuilder, Action{MemoryDocumentCacheOptions})"/>
    public static IGraphQLBuilder UseMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions, IServiceProvider>? action)
    {
        builder.Services.Configure(action);
        return builder.ConfigureExecution<MemoryDocumentCache>();
    }

    /// <summary>
    /// Adds support of Automatic Persisted Queries; see
    /// <see href="https://www.apollographql.com/docs/react/api/link/persisted-queries/"/>.
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

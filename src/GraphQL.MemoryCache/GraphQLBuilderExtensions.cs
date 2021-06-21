using System;
using GraphQL.DI;

namespace GraphQL.Caching
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Registers <see cref="MemoryDocumentCache"/> as a singleton of type <see cref="IDocumentCache"/> within the
        /// dependency injection framework, and configures it with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions> action = null)
            => builder.AddDocumentCache<MemoryDocumentCache>().Configure(action);

        /// <inheritdoc cref="AddMemoryCache(IGraphQLBuilder, Action{MemoryDocumentCacheOptions})"/>
        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions, IServiceProvider> action)
            => builder.AddDocumentCache<MemoryDocumentCache>().Configure(action);
    }
}

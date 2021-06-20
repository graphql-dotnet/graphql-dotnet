using System;
using GraphQL.DI;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions> action = null)
            => builder.AddDocumentCache<MemoryDocumentCache>().Configure(action);

        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Action<MemoryDocumentCacheOptions, IServiceProvider> action)
            => builder.AddDocumentCache<MemoryDocumentCache>().Configure(action);
    }
}

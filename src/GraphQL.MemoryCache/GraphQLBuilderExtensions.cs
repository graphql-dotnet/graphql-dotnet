using System;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder)
        {
            builder.Register<IDocumentCache, MemoryDocumentCache>(ServiceLifetime.Singleton);
            return builder;
        }

        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Func<IServiceProvider, MemoryDocumentCacheOptions> optionsFactory)
        {
            builder.Register<IDocumentCache, MemoryDocumentCache>(ServiceLifetime.Singleton);
            builder.Register<IOptions<MemoryDocumentCacheOptions>>(ServiceLifetime.Singleton, optionsFactory);
            return builder;
        }

        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, MemoryDocumentCacheOptions options)
        {
            builder.Register<IDocumentCache, MemoryDocumentCache>(ServiceLifetime.Singleton);
            builder.Register<IOptions<MemoryDocumentCacheOptions>>(ServiceLifetime.Singleton, _ => options);
            return builder;
        }
    }
}

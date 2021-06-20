using System;
using GraphQL.DI;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder)
        {
            builder.AddDocumentCache<MemoryDocumentCache>();
            return builder;
        }

        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, Func<IServiceProvider, MemoryDocumentCacheOptions> optionsFactory)
        {
            builder.AddDocumentCache<MemoryDocumentCache>();
            builder.Register<IOptions<MemoryDocumentCacheOptions>>(ServiceLifetime.Singleton, optionsFactory);
            return builder;
        }

        public static IGraphQLBuilder AddMemoryCache(this IGraphQLBuilder builder, MemoryDocumentCacheOptions options)
        {
            builder.AddDocumentCache<MemoryDocumentCache>();
            builder.Register<IOptions<MemoryDocumentCacheOptions>>(ServiceLifetime.Singleton, _ => options);
            return builder;
        }
    }
}

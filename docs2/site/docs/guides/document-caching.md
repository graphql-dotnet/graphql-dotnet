# Document Caching

In order to process a GraphQL request, the incoming request must be parsed and validated prior to execution. The parsed and validated
request may be cached in order to save execution time the next time the same request is executed. For usage patterns where the same
requests are executed repeatedly, caching can be enabled in order to increase throughput at the cost of memory use. As this may be
detrimental to performance for certain workloads, it is disabled by default.

Document caching is provided through the `IDocumentCache` interface. To enable document caching, you will need to construct the document
executer instance with a `IDocumentCache` implementation. There is a memory-backed implementation called `MemoryDocumentCache` in the NuGet
[GraphQL.Caching](https://www.nuget.org/packages/GraphQL.Caching/) package. The implementation is backed by
`Microsoft.Extensions.Caching.Memory.IMemoryCache` and provides options for specifying the maximum amount of objects to cache
(measured in total length of the cached queries), and/or the expiration time of cached queries.

Below are samples of how to use the caching engine:

```cs
var memoryDocumentCache = new MemoryDocumentCache(new MemoryDocumentCacheOptions {
    // maximum total cached query length of 1,000,000 bytes (assume 10x memory usage for 10MB maximum memory use by the cache)
    SizeLimit = 1000000,
    // no expiration of cached queries (cached queries are only ejected when the cache is full)
    SlidingExpiration = null,
});

var executer = new DocumentExecuter(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer(), memoryDocumentCache);
```

If you utilize dependency injection, register the memory cache and document executer as singletons. Below is a sample for the
Microsoft dependency injection service provider:

```cs
services.AddSingleton<IDocumentCache>(services =>
{
    return new MemoryDocumentCache(new MemoryDocumentCacheOptions {
        // maximum total cached query length of 1,000,000 bytes (assume 10x memory usage for 10MB maximum memory use by the cache)
        SizeLimit = 1000000,
        // no expiration of cached queries (cached queries are only ejected when the cache is full)
        SlidingExpiration = null,
    });
});

services.AddSingleton<IDocumentExecuter>(services =>
{
    var memoryDocumentCache = services.GetRequiredService<IDocumentCache>();

    return new DocumentExecuter(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer(), memoryDocumentCache);
});
```

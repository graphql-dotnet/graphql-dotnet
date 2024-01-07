# Document Caching

In order to process a GraphQL request, the incoming request must be parsed and validated prior to execution. The parsed and validated
document may be cached in order to save execution time the next time the same document is executed. Note that the
request may select a different operation or have different variables but still be the same document. For usage patterns where the same
document is executed repeatedly, caching can be enabled in order to increase throughput at the cost of memory use. As this may be
detrimental for performance for certain workloads, it is disabled by default.

The recommended way to enable document caching is to inherit from `IConfigureExecution` interface and register your class as its
implementation. There is a memory-backed implementation called `MemoryDocumentCache` in the NuGet
[GraphQL.MemoryCache](https://www.nuget.org/packages/GraphQL.MemoryCache) package. The implementation is backed by
`Microsoft.Extensions.Caching.Memory.IMemoryCache` and provides options for specifying the maximum amount of objects to cache
(measured in total length of the cached queries), and/or the expiration time of cached queries.

Below is a sample of how to use the caching engine:

```csharp
IServiceCollection services = ...;
services.AddGraphQL(builder => builder
    .AddSchema<StarWarsSchema>()
    .AddSystemTextJson()
    .UseMemoryCache(options =>
    {
        // maximum total cached query length of 1,000,000 bytes (assume 10x memory usage
        // for 10MB maximum memory use by the cache - parsed AST and other stuff)
        options.SizeLimit = 1000000;
        // no expiration of cached queries (cached queries are only ejected when the cache is full)
        options.SlidingExpiration = null;
    })
```

## Notes

If literal values are passed as arguments to a query, those literals are part of the cached document, so a
similar request with different argument literals will be parsed, validated, and cached separately. So it is very
important to provide arguments via variables and not as literals within the query (unless the arguments are constants).

Document caching assumes that validation rules do not depend on the inputs or user context for the execution. Further,
documents are not cached unless they pass validation. So it is assumed that validation need not run on queries that
have been cached. If you have custom validation rules that examine the user context or inputs, you will want to add
those validation rules to `ExecutionOptions.CachedDocumentValidationRules` so they run for every execution.

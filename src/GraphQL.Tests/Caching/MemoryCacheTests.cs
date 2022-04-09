using GraphQL.Caching;
using GraphQLParser.AST;

namespace GraphQL.Tests.Caching;

public class MemoryCacheTests
{
    [Fact]
    public async Task Validate_Entry_Is_Cached()
    {
        var doc = new GraphQLDocument();
        var query = "test";
        var memoryCache = new MemoryDocumentCache();

        (await memoryCache.GetAsync(query).ConfigureAwait(false)).ShouldBeNull();

        await memoryCache.SetAsync(query, doc).ConfigureAwait(false);
        (await memoryCache.GetAsync(query).ConfigureAwait(false)).ShouldBe(doc);
    }

    [Fact]
    public async Task Validate_Cache_Cannot_Be_Removed_Or_Set_To_Null()
    {
        var doc = new GraphQLDocument();
        var query = "test";
        var memoryCache = new MemoryDocumentCache();

        await memoryCache.SetAsync(query, doc).ConfigureAwait(false);

        await Should.ThrowAsync<ArgumentNullException>(async () => await memoryCache.SetAsync(query, null).ConfigureAwait(false)).ConfigureAwait(false);

        (await memoryCache.GetAsync(query).ConfigureAwait(false)).ShouldBe(doc);
    }
}

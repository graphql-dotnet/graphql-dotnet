using GraphQL.Caching;
using GraphQLParser.AST;

namespace GraphQL.Tests.Caching;

public class MemoryCacheTests
{
    [Fact]
    public async Task Validate_Entry_Is_Cached()
    {
        var doc = new GraphQLDocument();
        var options = new ExecutionOptions { Query = "test" };
        var memoryCache = new MemoryDocumentCache();

        (await memoryCache.GetAsync(options).ConfigureAwait(false)).ShouldBeNull();

        await memoryCache.SetAsync(options, doc).ConfigureAwait(false);
        (await memoryCache.GetAsync(options).ConfigureAwait(false)).ShouldBe(doc);
    }

    [Fact]
    public async Task Validate_Cache_Cannot_Be_Removed_Or_Set_To_Null()
    {
        var doc = new GraphQLDocument();
        var options = new ExecutionOptions { Query = "test" };
        var memoryCache = new MemoryDocumentCache();

        await memoryCache.SetAsync(options, doc).ConfigureAwait(false);

        await Should.ThrowAsync<ArgumentNullException>(async () => await memoryCache.SetAsync(options, null).ConfigureAwait(false)).ConfigureAwait(false);

        (await memoryCache.GetAsync(options).ConfigureAwait(false)).ShouldBe(doc);
    }
}

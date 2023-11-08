#nullable enable

using GraphQL.Caching;
using GraphQLParser.AST;

namespace GraphQL.Tests.Caching;

public class MemoryCacheTests
{
    internal class MyMemoryDocumentCache : MemoryDocumentCache
    {
        public ValueTask<GraphQLDocument?> GetAsyncPublic(ExecutionOptions options) => GetAsync(options);

        public ValueTask SetAsyncPublic(ExecutionOptions options, GraphQLDocument value) => SetAsync(options, value);
    }

    [Fact]
    public async Task Validate_Entry_Is_Cached()
    {
        var doc = new GraphQLDocument(new());
        var options = new ExecutionOptions { Query = "test" };
        var memoryCache = new MyMemoryDocumentCache();

        (await memoryCache.GetAsyncPublic(options)).ShouldBeNull();

        await memoryCache.SetAsyncPublic(options, doc);
        (await memoryCache.GetAsyncPublic(options)).ShouldBe(doc);
    }

    [Fact]
    public async Task Validate_Cache_Cannot_Be_Removed_Or_Set_To_Null()
    {
        var doc = new GraphQLDocument(new());
        var options = new ExecutionOptions { Query = "test" };
        var memoryCache = new MyMemoryDocumentCache();

        await memoryCache.SetAsyncPublic(options, doc);

        await Should.ThrowAsync<ArgumentNullException>(async () => await memoryCache.SetAsyncPublic(options, null!));

        (await memoryCache.GetAsyncPublic(options)).ShouldBe(doc);
    }
}

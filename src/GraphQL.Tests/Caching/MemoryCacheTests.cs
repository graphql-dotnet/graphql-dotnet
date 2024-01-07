using GraphQL.Caching;
using GraphQLParser.AST;
using Moq;
using Moq.Protected;

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
        var doc = new GraphQLDocument();
        var options = new ExecutionOptions { Query = "test" };
        var memoryCache = new MyMemoryDocumentCache();

        (await memoryCache.GetAsyncPublic(options)).ShouldBeNull();

        await memoryCache.SetAsyncPublic(options, doc);
        (await memoryCache.GetAsyncPublic(options)).ShouldBe(doc);
    }

    [Fact]
    public async Task Validate_Cache_Cannot_Be_Removed_Or_Set_To_Null()
    {
        var doc = new GraphQLDocument();
        var options = new ExecutionOptions { Query = "test" };
        var memoryCache = new MyMemoryDocumentCache();

        await memoryCache.SetAsyncPublic(options, doc);

        await Should.ThrowAsync<ArgumentNullException>(async () => await memoryCache.SetAsyncPublic(options, null!));

        (await memoryCache.GetAsyncPublic(options)).ShouldBe(doc);
    }

    [Theory]
    // no query set
    [InlineData(false, false, false, false, true, true, false)]
    // doc already set
    [InlineData(false, true, false, false, true, true, false)]
    [InlineData(true, true, false, false, true, true, false)]
    // typical path with cache miss
    [InlineData(true, false, true, false, true, true, true)] // passed validation
    [InlineData(true, false, true, false, false, true, false)] // failed validation
    [InlineData(true, false, true, false, true, false, false)] // didn't set document (should not be possible)
    [InlineData(true, false, true, false, false, false, false)] // failed parse
    // typical path with cache hit; should never call SetAsync
    [InlineData(true, false, true, true, true, true, false)]
    [InlineData(true, false, true, true, false, true, false)]
    [InlineData(true, false, true, true, true, false, false)]
    [InlineData(true, false, true, true, false, false, false)]
    public async Task ExecuteAsync(bool querySet, bool docSet, bool getCalled, bool getReturned, bool executed, bool exectuedSetDocument, bool setCalled)
    {
        var mockDocument = new GraphQLDocument();
        var options = new ExecutionOptions
        {
            Query = querySet ? "Some Query" : null,
            Document = docSet ? mockDocument : null,
        };

        var memoryDocumentCacheMock = new Mock<MemoryDocumentCache>() { CallBase = true };
        memoryDocumentCacheMock.Protected()
            .Setup<ValueTask<GraphQLDocument?>>("GetAsync", ItExpr.IsAny<ExecutionOptions>())
            .Returns<ExecutionOptions>(opts =>
            {
                opts.ShouldBe(options);
                if (getReturned)
                    return new(mockDocument);
                return default;
            });

        memoryDocumentCacheMock.Protected()
            .Setup<ValueTask>("SetAsync", ItExpr.IsAny<ExecutionOptions>(), ItExpr.IsAny<GraphQLDocument>())
            .Returns<ExecutionOptions, GraphQLDocument>((opts, doc) =>
            {
                opts.ShouldBe(options);
                doc.ShouldBe(mockDocument);
                return default;
            });

        var result = new ExecutionResult()
        {
            Executed = executed,
            Document = exectuedSetDocument ? mockDocument : null,
        };

        var ret = await memoryDocumentCacheMock.Object.ExecuteAsync(options, (opts) =>
        {
            opts.ShouldBe(options);
            return Task.FromResult(result);
        });

        ret.ShouldBe(result);

        memoryDocumentCacheMock.Protected().Verify("GetAsync", getCalled ? Times.Once() : Times.Never(), ItExpr.IsAny<ExecutionOptions>());
        memoryDocumentCacheMock.Protected().Verify("SetAsync", setCalled ? Times.Once() : Times.Never(), ItExpr.IsAny<ExecutionOptions>(), ItExpr.IsAny<GraphQLDocument>());
    }
}

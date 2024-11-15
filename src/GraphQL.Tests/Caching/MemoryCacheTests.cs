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

    /// <summary>
    /// Executes multiple scenarios to verify that caching works correctly with Query strings and DocumentIds.
    /// </summary>
    /// <param name="cacheQuery">The Query string used when setting the cache. Can be null.</param>
    /// <param name="cacheDocumentId">The DocumentId used when setting the cache. Can be null.</param>
    /// <param name="retrieveQuery">The Query string used when retrieving from the cache. Can be null.</param>
    /// <param name="retrieveDocumentId">The DocumentId used when retrieving from the cache. Can be null.</param>
    /// <param name="expectCached">Indicates whether the retrieval is expected to find the cached document.</param>
    [Theory]
    [InlineData("query1", null, "query1", null, true)]   // Cache by Query, retrieve by same Query
    [InlineData("query1", null, "query2", null, false)]  // Cache by Query, retrieve by different Query
    [InlineData(null, "doc1", null, "doc1", true)]       // Cache by DocumentId, retrieve by same DocumentId
    [InlineData(null, "doc1", null, "doc2", false)]      // Cache by DocumentId, retrieve by different DocumentId
    [InlineData("query1", "doc1", "query1", "doc1", true)] // Cache by both, retrieve by both
    [InlineData("query1", "doc1", "query1", "doc2", false)] // Cache by both, retrieve with different DocumentId
    [InlineData("query1", "doc1", "query2", "doc1", false)] // Cache by both, retrieve with different Query
    [InlineData("query1", "doc1", "query2", "doc2", false)] // Cache by both, retrieve with different Query and DocumentId
    [InlineData("query1", null, null, null, false)]      // Cache by Query, retrieve without Query or DocumentId
    [InlineData(null, "doc1", "query1", "doc1", false)]  // Cache by DocumentId, retrieve with Query and same DocumentId
    [InlineData("query1", "doc1", "query1", null, false)] // Cache by both, retrieve with only Query
    [InlineData("query1", "doc1", null, "doc1", false)]   // Cache by both, retrieve with only DocumentId
    [InlineData(null, null, "query1", null, false)]       // Cache by neither, retrieve with only Query
    [InlineData(null, null, null, "doc1", false)]         // Cache by neither, retrieve with only DocumentId
    [InlineData(null, null, null, null, false)]           // Cache by neither, retrieve with neither
    public async Task GetAsync_And_SetAsync_Should_Handle_Query_And_DocumentId_Correctly(
        string? cacheQuery,
        string? cacheDocumentId,
        string? retrieveQuery,
        string? retrieveDocumentId,
        bool expectCached)
    {
        // Arrange
        var document = new GraphQLDocument(new());
        var cacheOptions = new ExecutionOptions
        {
            Query = cacheQuery,
            DocumentId = cacheDocumentId
        };
        var retrieveOptions = new ExecutionOptions
        {
            Query = retrieveQuery,
            DocumentId = retrieveDocumentId
        };
        var memoryCache = new MyMemoryDocumentCache();

        // Act
        var initialDocument = await memoryCache.GetAsyncPublic(retrieveOptions);

        if (cacheQuery != null || cacheDocumentId != null)
        {
            await memoryCache.SetAsyncPublic(cacheOptions, document);
        }

        var cachedDocument = await memoryCache.GetAsyncPublic(retrieveOptions);

        // Assert
        initialDocument.ShouldBeNull();
        if (expectCached)
        {
            cachedDocument.ShouldBe(document);
        }
        else
        {
            cachedDocument.ShouldBeNull();
        }
    }

    [Theory]
    // no query set
    [InlineData(false, false, false, false, false, true, true, false)]
    // doc already set
    [InlineData(false, false, true, false, false, true, true, false)]
    [InlineData(true, false, true, false, false, true, true, false)]
    [InlineData(false, true, true, false, false, true, true, false)]
    [InlineData(true, true, true, false, false, true, true, false)]
    // typical path with cache miss
    [InlineData(true, false, false, true, false, true, true, true)] // passed validation
    [InlineData(true, false, false, true, false, false, true, false)] // failed validation
    [InlineData(true, false, false, true, false, true, false, false)] // didn't set document (should not be possible)
    [InlineData(true, false, false, true, false, false, false, false)] // failed parse
    [InlineData(false, true, false, true, false, true, true, true)] // passed validation
    [InlineData(false, true, false, true, false, false, true, false)] // failed validation
    [InlineData(false, true, false, true, false, true, false, false)] // didn't set document (should not be possible)
    [InlineData(false, true, false, true, false, false, false, false)] // failed parse
    [InlineData(true, true, false, true, false, true, true, true)] // passed validation
    [InlineData(true, true, false, true, false, false, true, false)] // failed validation
    [InlineData(true, true, false, true, false, true, false, false)] // didn't set document (should not be possible)
    [InlineData(true, true, false, true, false, false, false, false)] // failed parse
    // typical path with cache hit; should never call SetAsync
    [InlineData(true, false, false, true, true, true, true, false)]
    [InlineData(true, false, false, true, true, false, true, false)]
    [InlineData(true, false, false, true, true, true, false, false)]
    [InlineData(true, false, false, true, true, false, false, false)]
    [InlineData(false, true, false, true, true, true, true, false)]
    [InlineData(false, true, false, true, true, false, true, false)]
    [InlineData(false, true, false, true, true, true, false, false)]
    [InlineData(false, true, false, true, true, false, false, false)]
    [InlineData(true, true, false, true, true, true, true, false)]
    [InlineData(true, true, false, true, true, false, true, false)]
    [InlineData(true, true, false, true, true, true, false, false)]
    [InlineData(true, true, false, true, true, false, false, false)]
    public async Task ExecuteAsync(bool querySet, bool documentIdSet, bool docSet, bool getCalled, bool getReturned, bool executed, bool exectuedSetDocument, bool setCalled)
    {
        var mockDocument = new GraphQLDocument(new());
        var options = new ExecutionOptions
        {
            Query = querySet ? "Some Query" : null,
            DocumentId = documentIdSet ? "Some Document Id" : null,
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

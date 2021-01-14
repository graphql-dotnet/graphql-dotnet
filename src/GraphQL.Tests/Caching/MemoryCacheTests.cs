using GraphQL.Caching;
using GraphQL.Language.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Caching
{
    public class MemoryCacheTests
    {
        [Fact]
        public void ValidateEntryIsCached()
        {
            var doc = new Document();
            var query = "test";
            var memoryCache = new MemoryDocumentCache();
            memoryCache[query].ShouldBeNull();
            memoryCache[query] = doc;
            memoryCache[query].ShouldBe(doc);
        }
    }
}

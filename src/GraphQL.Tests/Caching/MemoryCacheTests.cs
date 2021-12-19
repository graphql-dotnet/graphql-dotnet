using System.Threading.Tasks;
using GraphQL.Caching;
using GraphQL.Language.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Caching
{
    public class MemoryCacheTests
    {
        [Fact]
        public async Task Validate_Entry_Is_Cached()
        {
            var doc = new Document();
            var query = "test";
            var memoryCache = new MemoryDocumentCache();

            (await memoryCache.GetAsync(query)).ShouldBeNull();

            await memoryCache.SetAsync(query, doc);
            (await memoryCache.GetAsync(query)).ShouldBe(doc);
        }
    }
}

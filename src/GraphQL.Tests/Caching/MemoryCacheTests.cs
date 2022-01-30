using System;
using System.Threading.Tasks;
using GraphQL.Caching;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Caching
{
    public class MemoryCacheTests
    {
        [Fact]
        public async Task Validate_Entry_Is_Cached()
        {
            var doc = new GraphQLDocument();
            var query = "test";
            var memoryCache = new MemoryDocumentCache();

            (await memoryCache.GetAsync(query)).ShouldBeNull();

            await memoryCache.SetAsync(query, doc);
            (await memoryCache.GetAsync(query)).ShouldBe(doc);
        }

        [Fact]
        public async Task Validate_Cache_Cannot_Be_Removed_Or_Set_To_Null()
        {
            var doc = new GraphQLDocument();
            var query = "test";
            var memoryCache = new MemoryDocumentCache();

            await memoryCache.SetAsync(query, doc);

            await Should.ThrowAsync<ArgumentNullException>(async () => await memoryCache.SetAsync(query, null));

            (await memoryCache.GetAsync(query)).ShouldBe(doc);
        }
    }
}

using GraphQL.Caching;
using GraphQLParser.AST;

namespace GraphQL.Tests.Caching
{
    public class DefaultCacheTests
    {
        [Fact]
        public async Task Should_Never_Get_A_Cache()
        {
            var doc = new GraphQLDocument();
            var query = "test";
            var memoryCache = DefaultDocumentCache.Instance;

            (await memoryCache.GetAsync(query)).ShouldBeNull();

            await memoryCache.SetAsync(query, doc);
            (await memoryCache.GetAsync(query)).ShouldBeNull();
        }
    }
}

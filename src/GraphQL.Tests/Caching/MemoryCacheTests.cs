using System;
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
            var memoryCache = new MemoryDocumentCache(100000, TimeSpan.FromHours(1));
            memoryCache[query].ShouldBeNull();
            memoryCache[query] = doc;
            memoryCache[query].ShouldBe(doc);
        }
    }
}

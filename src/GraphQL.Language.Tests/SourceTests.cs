using Xunit;

namespace GraphQL.Language.Tests
{
    public class SourceTests
    {
        [Fact]
        public void CreateSourceFromString_BodyEqualsToProvidedSource()
        {
            var source = new Source("somesrc");

            Assert.Equal("somesrc", source.Body);
        }

        [Fact]
        public void CreateSourceFromString_SourceNameEqualsToGraphQL()
        {
            var source = new Source("somesrc");

            Assert.Equal("GraphQL", source.Name);
        }

        [Fact]
        public void CreateSourceFromStringWithName_SourceNameEqualsToProvidedName()
        {
            var source = new Source("somesrc", "somename");

            Assert.Equal("somename", source.Name);
        }
    }
}

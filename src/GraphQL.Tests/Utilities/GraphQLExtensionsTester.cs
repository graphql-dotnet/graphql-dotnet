using Should;

namespace GraphQL.Tests
{
    public class GraphQLExtensionsTester
    {
        [Fact]
        public void trims_nonnull_bang()
        {
            var nonNullName = "Human!";

            nonNullName.TrimGraphQLTypes().ShouldEqual("Human");
        }

        [Fact]
        public void trims_array()
        {
            var nonNullName = "[Human]";

            nonNullName.TrimGraphQLTypes().ShouldEqual("Human");
        }

        [Fact]
        public void trims_combo()
        {
            var nonNullName = "[Human]!";

            nonNullName.TrimGraphQLTypes().ShouldEqual("Human");
        }
    }
}

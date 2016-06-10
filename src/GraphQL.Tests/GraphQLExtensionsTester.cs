using Should;

namespace GraphQL.Tests
{
    public class GraphQLExtensionsTester
    {
        [Test]
        public void trims_nonnull_bang()
        {
            var nonNullName = "Human!";

            nonNullName.TrimGraphQLTypes().ShouldEqual("Human");
        }

        [Test]
        public void trims_array()
        {
            var nonNullName = "[Human]";

            nonNullName.TrimGraphQLTypes().ShouldEqual("Human");
        }

        [Test]
        public void trims_combo()
        {
            var nonNullName = "[Human]!";

            nonNullName.TrimGraphQLTypes().ShouldEqual("Human");
        }
    }
}

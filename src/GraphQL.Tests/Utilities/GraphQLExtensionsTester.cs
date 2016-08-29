using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class GraphQLExtensionsTester
    {
        [Fact]
        public void trims_nonnull_bang()
        {
            var nonNullName = "Human!";

            nonNullName.TrimGraphQLTypes().ShouldBe("Human");
        }

        [Fact]
        public void trims_array()
        {
            var nonNullName = "[Human]";

            nonNullName.TrimGraphQLTypes().ShouldBe("Human");
        }

        [Fact]
        public void trims_combo()
        {
            var nonNullName = "[Human]!";

            nonNullName.TrimGraphQLTypes().ShouldBe("Human");
        }
    }
}

using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class GraphQLExtensionsTester
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("Human", "Human")]
        [InlineData("Human!", "Human")]
        [InlineData("[Human]", "Human")]
        [InlineData("[Human]!", "Human")]
        [InlineData("[[Human!]!]!", "Human")]
        [InlineData("[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[]]]]]]]]]]]]]]]]]]]]]]]]]]]]]Human!!!!!!!!!!!!!]!!!!!!]]]]]]]!", "Human")]
        public void nothing_to_trim(string sourceName, string expectedName)
        {
            sourceName.TrimGraphQLTypes().ShouldBe(expectedName);
        }
    }
}

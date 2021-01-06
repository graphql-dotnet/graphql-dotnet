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
        public void TrimGraphQLTypes_Should_Return_Expected_Results(string sourceName, string expectedName)
        {
            sourceName.TrimGraphQLTypes().ShouldBe(expectedName);
        }
    }
}

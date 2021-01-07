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

        [Theory]
        [InlineData("", "")]
        [InlineData(" ", "")]
        [InlineData("\tAA", "\tAA")] // ???
        [InlineData("a", "a")]
        [InlineData("Aa", "aa")]
        [InlineData("9a", "9a")]
        [InlineData("aBC", "aBC")]
        public void ToCamelCase_Should_Return_Expected_Results(string sourceName, string expectedName)
        {
            sourceName.ToCamelCase().ShouldBe(expectedName);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData(" ", "")]
        [InlineData("\tAA", "\tAA")] // ???
        [InlineData("a", "A")]
        [InlineData("Aa", "Aa")]
        [InlineData("9a", "9a")]
        [InlineData("aBC", "ABC")]
        public void ToPascalCase_Should_Return_Expected_Results(string sourceName, string expectedName)
        {
            sourceName.ToPascalCase().ShouldBe(expectedName);
        }
    }
}

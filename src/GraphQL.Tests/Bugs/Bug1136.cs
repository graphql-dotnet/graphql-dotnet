using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using System.Linq;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug1136
    {
        [Fact]
        public void IsValidLiteralValue_Should_Not_Throw_NRE()
        {
            var type = new DateGraphType() { Name = null };
            var value = new BooleanValue(true);
            var result = type.IsValidLiteralValue(value, null).ToArray();
            result.Length.ShouldBe(1);
            result[0].ShouldBe("Expected type \"null\", found true.");
        }
    }
}

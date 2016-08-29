using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class BooleanGraphTypeTests
    {
        private BooleanGraphType type = new BooleanGraphType();

        [Fact]
        public void coerces_0_to_false()
        {
            type.ParseValue(0).ShouldBe(false);
        }

        [Fact]
        public void coerces_1_to_true()
        {
            type.ParseValue(1).ShouldBe(true);
        }

        [Fact]
        public void coerces_string_false()
        {
            type.ParseValue("false").ShouldBe(false);
        }

        [Fact]
        public void coerces_string_False()
        {
            type.ParseValue("False").ShouldBe(false);
        }

        [Fact]
        public void coerces_string_true()
        {
            type.ParseValue("true").ShouldBe(true);
        }

        [Fact]
        public void coerces_string_True()
        {
            type.ParseValue("True").ShouldBe(true);
        }
    }
}

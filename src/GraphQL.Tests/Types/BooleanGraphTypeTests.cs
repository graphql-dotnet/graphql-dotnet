using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class BooleanGraphTypeTests
    {
        private BooleanGraphType type = new BooleanGraphType();

        [Fact]
        public void coerces_0_to_false()
        {
            type.ParseValue(0).ShouldEqual(false);
        }

        [Fact]
        public void coerces_1_to_true()
        {
            type.ParseValue(1).ShouldEqual(true);
        }

        [Fact]
        public void coerces_string_false()
        {
            type.ParseValue("false").ShouldEqual(false);
        }

        [Fact]
        public void coerces_string_False()
        {
            type.ParseValue("False").ShouldEqual(false);
        }

        [Fact]
        public void coerces_string_true()
        {
            type.ParseValue("true").ShouldEqual(true);
        }

        [Fact]
        public void coerces_string_True()
        {
            type.ParseValue("True").ShouldEqual(true);
        }
    }
}

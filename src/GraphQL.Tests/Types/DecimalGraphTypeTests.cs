using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class DecimalGraphTypeTests
    {
        private DecimalGraphType type = new DecimalGraphType();

        [Fact]
        public void coerces_null_to_null()
        {
            type.ParseValue(null).ShouldEqual(null);
        }

        [Fact]
        public void coerces_integer_to_decimal()
        {
            type.ParseValue(0).ShouldEqual((decimal)0);
        }

        [Fact]
        public void coerces_invalid_string_to_null()
        {
            type.ParseValue("abcd").ShouldEqual(null);
        }

        [Fact]
        public void coerces_numeric_string_to_decimal()
        {
            type.ParseValue("12345").ShouldEqual((decimal)12345);
        }
    }
}

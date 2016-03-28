using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class DecimalGraphTypeTests
    {
        private DecimalGraphType type = new DecimalGraphType();

        [Test]
        public void coerces_null_to_null()
        {
            type.ParseValue(null).ShouldEqual(null);
        }

        [Test]
        public void coerces_integer_to_decimal()
        {
            type.ParseValue(0).ShouldEqual((decimal)0);
        }

        [Test]
        public void coerces_invalid_string_to_null()
        {
            type.ParseValue("abcd").ShouldEqual(null);
        }

        [Test]
        public void coerces_numeric_string_to_decimal()
        {
            type.ParseValue("12345").ShouldEqual((decimal)12345);
        }
    }
}

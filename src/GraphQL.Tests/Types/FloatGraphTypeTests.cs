using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class FloatGraphTypeTests
    {
        private FloatGraphType type = new FloatGraphType();

        [Test]
        public void coerces_null_to_null()
        {
            type.ParseValue(null).ShouldEqual(null);
        }

        [Test]
        public void coerces_invalid_string_to_null()
        {
            type.ParseValue("abcd").ShouldEqual(null);
        }

        [Test]
        public void coerces_double_to_value()
        {
            type.ParseValue(1.79769313486231e308).ShouldEqual((double)1.79769313486231e308);
        }
    }
}

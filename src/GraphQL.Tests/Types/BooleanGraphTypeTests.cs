using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class BooleanGraphTypeTests
    {
        private BooleanGraphType type = new BooleanGraphType();

        [Test]
        public void coerces_0_to_false()
        {
            type.ParseValue(0).ShouldEqual(false);
        }

        [Test]
        public void coerces_1_to_true()
        {
            type.ParseValue(1).ShouldEqual(true);
        }

        [Test]
        public void coerces_string_false()
        {
            type.ParseValue("false").ShouldEqual(false);
        }

        [Test]
        public void coerces_string_False()
        {
            type.ParseValue("False").ShouldEqual(false);
        }

        [Test]
        public void coerces_string_true()
        {
            type.ParseValue("true").ShouldEqual(true);
        }

        [Test]
        public void coerces_string_True()
        {
            type.ParseValue("True").ShouldEqual(true);
        }
    }
}

using Should;

namespace GraphQL.Tests.Types
{
    public class BooleanGraphTypeTests
    {
        private BooleanGraphType type = new BooleanGraphType();

        [Test]
        public void coerces_0_to_false()
        {
            type.Coerce(0).ShouldEqual(false);
        }

        [Test]
        public void coerces_1_to_true()
        {
            type.Coerce(1).ShouldEqual(true);
        }

        [Test]
        public void coerces_string_false()
        {
            type.Coerce("false").ShouldEqual(false);
        }

        [Test]
        public void coerces_string_False()
        {
            type.Coerce("False").ShouldEqual(false);
        }

        [Test]
        public void coerces_string_true()
        {
            type.Coerce("true").ShouldEqual(true);
        }

        [Test]
        public void coerces_string_True()
        {
            type.Coerce("True").ShouldEqual(true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            type.Coerce(null).ShouldEqual(null);
        }

        [Test]
        public void coerces_invalid_string_to_null()
        {
            type.Coerce("abcd").ShouldEqual(null);
        }

        [Test]
        public void coerces_double_to_value()
        {
            type.Coerce(1.79769313486231e308).ShouldEqual((double)1.79769313486231e308);
        }
    }
}

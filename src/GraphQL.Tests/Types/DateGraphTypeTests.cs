using System;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class DateGraphTypeTests
    {
        private DateGraphType type = new DateGraphType();

        [Test]
        public void coerces_integer_to_null()
        {
            type.Coerce(0).ShouldEqual(null);
        }

        [Test]
        public void coerces_invalid_string_to_null()
        {
            type.Coerce("some unknown date").ShouldEqual(null);
        }

        [Test]
        public void coerces_invalidly_formatted_date_to_null()
        {
            type.Coerce("Dec 14 2012").ShouldEqual(null);
        }

        [Test]
        public void coerces_iso8601_formatted_string_to_date()
        {
            type.Coerce("2015-12-01T14:15:07.12345Z").ShouldEqual(
                new DateTime(2015, 12, 01, 14, 15, 7) + TimeSpan.FromMilliseconds(12345));
        }

        [Test]
        public void coerces_iso8601_string_with_tzone_to_date()
        {
            type.Coerce("2015-11-21T19:59:32.012+0200").ShouldEqual(
                new DateTime(2015, 11, 21, 17, 59, 32) + TimeSpan.FromMilliseconds(012));
        }
    }
}

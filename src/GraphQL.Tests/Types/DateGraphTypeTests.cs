using System;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class DateGraphTypeTests
    {
        private DateGraphType type = new DateGraphType();

        [Fact]
        public void coerces_integer_to_null()
        {
            type.ParseValue(0).ShouldEqual(null);
        }

        [Fact]
        public void coerces_invalid_string_to_null()
        {
            type.ParseValue("some unknown date").ShouldEqual(null);
        }

        [Fact]
        public void coerces_invalidly_formatted_date_to_null()
        {
            type.ParseValue("Dec 32 2012").ShouldEqual(null);
        }

        [Fact]
        public void coerces_iso8601_formatted_string_to_date()
        {
            type.ParseValue("2015-12-01T14:15:07.123Z").ShouldEqual(
                new DateTime(2015, 12, 01, 14, 15, 7) + TimeSpan.FromMilliseconds(123));
        }

        [Fact]
        public void coerces_iso8601_string_with_tzone_to_date()
        {
            type.ParseValue("2015-11-21T19:59:32.987+0200").ShouldEqual(
                new DateTime(2015, 11, 21, 17, 59, 32) + TimeSpan.FromMilliseconds(987));
        }
    }
}

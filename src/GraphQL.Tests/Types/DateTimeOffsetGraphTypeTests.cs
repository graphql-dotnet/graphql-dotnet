using System;
using System.Globalization;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class DateTimeOffsetGraphTypeTests
    {
        private readonly DateTimeOffsetGraphType _type = new DateTimeOffsetGraphType();

        [Fact]
        public void coerces_valid_date()
        {
            CultureTestHelper.UseCultures(() =>
            {
                var expected = DateTimeOffset.UtcNow;
                var input = expected.ToString("O", DateTimeFormatInfo.InvariantInfo);

                var actual = _type.ParseValue(input);

                actual.ShouldBe(expected);
            });
        }

        [Fact]
        public void coerces_invalid_string_to_exception()
        {
            CultureTestHelper.UseCultures(() => Should.Throw<FormatException>(() => _type.ParseValue("some unknown date")));
        }

        [Fact]
        public void coerces_invalidly_formatted_date_to_exception()
        {
            CultureTestHelper.UseCultures(() => Should.Throw<FormatException>(() => _type.ParseValue("Dec 32 2012")));
        }

        [Fact]
        public void coerces_iso8601_utc_formatted_string_to_date()
        {
            CultureTestHelper.UseCultures(() =>
            {
                _type.ParseValue("2015-12-01T14:15:07.123Z").ShouldBe(
                    new DateTimeOffset(2015, 12, 01, 14, 15, 7, 123, TimeSpan.Zero));
            });
        }

        [Fact]
        public void coerces_iso8601_string_with_tzone_to_date()
        {
            CultureTestHelper.UseCultures(() =>
            {
                _type.ParseValue("2015-11-21T19:59:32.987+0200").ShouldBe(
                    new DateTimeOffset(2015, 11, 21, 19, 59, 32, 987, TimeSpan.FromHours(2)));
            });
        }
    }
}

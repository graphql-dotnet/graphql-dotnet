using System;
using System.Globalization;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class DateGraphTypeTests
    {
        private readonly DateGraphType _type = new DateGraphType();

        [Fact]
        public void serialize_string_to_date()
        {
            CultureTestHelper.UseCultures(() =>
            {
                var actual = _type.Serialize("2018-07-24");
                actual.ShouldBe("2018-07-24");
            });
        }

        [Fact]
        public void coerces_datetimes_to_utc()
        {
            CultureTestHelper.UseCultures(() =>
            {
                ((DateTime) _type.ParseValue("2015-11-21")).Kind.ShouldBe(
                    DateTimeKind.Utc);
            });
        }

        [Fact]
        public void coerces_invalid_string_to_exception()
        {
            CultureTestHelper.UseCultures(() =>
            {
                Should.Throw<FormatException>(() => _type.ParseValue("some unknown date"));
            });
        }

        [Fact]
        public void coerces_invalidly_formatted_date_to_exception()
        {
            CultureTestHelper.UseCultures(() =>
            {
                Should.Throw<FormatException>(() => _type.ParseValue("Dec 32 2012"));
            });
        }

        [Fact]
        public void coerces_iso8601_formatted_string_to_date()
        {
            CultureTestHelper.UseCultures(() =>
            {
                _type.ParseValue("2015-12-01").ShouldBe(
                    new DateTime(2015, 12, 01, 0, 0, 0));
            });
        }
    }
}

using System.Globalization;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class DateGraphTypeTests
{
    private readonly DateGraphType _type = new DateGraphType();

    [Fact]
    public void serialize_string_to_date_throws()
    {
        CultureTestHelper.UseCultures(() => Should.Throw<InvalidOperationException>(() => _type.Serialize("2018-07-24")));
    }

    [Fact]
    public void serialize_local_date_time_throws()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var date = new DateTime(2000, 1, 2, 3, 4, 5, 6, DateTimeKind.Local);
            Should.Throw<FormatException>(() => _type.Serialize(date));
        });
    }

    [Fact]
    public void serialize_utc_date_time_throws()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var date = new DateTime(2000, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            Should.Throw<FormatException>(() => _type.Serialize(date));
        });
    }

    [Fact]
    public void o_format_throws()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = DateTime.UtcNow;
            var input = expected.ToLocalTime().ToString("O", DateTimeFormatInfo.InvariantInfo);
            Should.Throw<FormatException>(() => _type.ParseValue(input));
        });
    }

    [Fact]
    public void coerces_datetimes_to_utc()
    {
        CultureTestHelper.UseCultures(() =>
        {
            ((DateTime)_type.ParseValue("2015-11-21")).Kind.ShouldBe(
                DateTimeKind.Utc);
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
    public void coerces_iso8601_formatted_string_to_date()
    {
        CultureTestHelper.UseCultures(() =>
        {
            _type.ParseValue("2015-12-01").ShouldBe(
                new DateTime(2015, 12, 01, 0, 0, 0));
        });
    }

    [Fact]
    public void coerces_iso8601_string_with_tzone_to_date()
    {
        CultureTestHelper.UseCultures(() => Should.Throw<FormatException>(() => _type.ParseValue("2015-11-21T19:59:32.987+0200")));
    }
}

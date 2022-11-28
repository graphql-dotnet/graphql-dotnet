using System.Globalization;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class DateTimeOffsetGraphTypeTests
{
    private readonly DateTimeOffsetGraphType _type = new();

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
            var dateTimeOffset = (DateTimeOffset)_type.ParseValue("2015-11-21T19:59:32.987+0200");
            dateTimeOffset.Date.ShouldBe(new DateTime(2015, 11, 21));
            dateTimeOffset.TimeOfDay.ShouldBe(new TimeSpan(0, 19, 59, 32, 987));
            dateTimeOffset.Offset.ShouldBe(TimeSpan.FromHours(2));
        });
    }

    public static object[][] DateTimeTypeTests()
    {
        var dateTimeNow = DateTime.Now;
        var dateTimeUtcNow = DateTime.UtcNow;
        var dateTimeUnspecified = new DateTime(2015, 11, 21, 17, 59, 32, DateTimeKind.Unspecified);

        return new object[][]
        {
            new object[] { dateTimeNow, new DateTimeOffset(dateTimeNow) },
            new object[] { dateTimeUtcNow, new DateTimeOffset(dateTimeUtcNow) },
            new object[] { dateTimeUnspecified, new DateTimeOffset(dateTimeUnspecified, TimeSpan.Zero) }
        };
    }

    [Theory]
    [MemberData(nameof(DateTimeTypeTests))]
    public void coerces_dateTime_type_to_date(DateTime input, DateTimeOffset expected)
    {
        CultureTestHelper.UseCultures(() => _type.ParseValue(input).ShouldBe(expected));
    }
}

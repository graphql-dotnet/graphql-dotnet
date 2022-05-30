#if NET6_0_OR_GREATER

using System.Globalization;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class DateOnlyGraphTypeTests
{
    private readonly DateOnlyGraphType _type = new();

    [Fact]
    public void coerces_valid_date()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = new DateOnly(2015, 12, 01);
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
    public void coerces_iso8601_formatted_string_to_date()
    {
        CultureTestHelper.UseCultures(() =>
        {
            _type.ParseValue("2015-12-01").ShouldBe(
                new DateOnly(2015, 12, 01));
        });
    }
}

#endif

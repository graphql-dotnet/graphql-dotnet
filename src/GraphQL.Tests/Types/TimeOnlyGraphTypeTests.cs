#if NET6_0_OR_GREATER

using System.Globalization;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class TimeOnlyGraphTypeTests
{
    private readonly TimeOnlyGraphType _type = new();

    [Fact]
    public void coerces_valid_time()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = new TimeOnly(10, 12, 01);
            var input = expected.ToString("O", DateTimeFormatInfo.InvariantInfo);

            var actual = _type.ParseValue(input);

            actual.ShouldBe(expected);
        });
    }

    [Fact]
    public void coerces_invalid_string_to_exception()
    {
        CultureTestHelper.UseCultures(() => Should.Throw<FormatException>(() => _type.ParseValue("some unknown time")));
    }

    [Fact]
    public void coerces_invalidly_formatted_time_to_exception()
    {
        CultureTestHelper.UseCultures(() =>
        {
            Should.Throw<FormatException>(() => _type.ParseValue("11 AM"));
            Should.Throw<FormatException>(() => _type.ParseValue("01:02:03.00400009"));
        });
    }

    [Fact]
    public void coerces_iso8601_formatted_string_to_time()
    {
        CultureTestHelper.UseCultures(() =>
        {
            _type.ParseValue("01:02:03.0040000").ShouldBe(new TimeOnly(1, 2, 3, 4));
            _type.ParseValue("01:02:03.0040009").ShouldBe(new TimeOnly(37230040009));
        });
    }
}

#endif

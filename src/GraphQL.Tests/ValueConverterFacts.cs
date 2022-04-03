using System.Globalization;

namespace GraphQL.Tests;

public class ValueConverterFacts
{
    [Theory]
    [InlineData(1234L, 1234.0)]
    public void LongConversions(long source, object expected)
    {
        var actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(12.5f, 12.5)]
    public void FloatConversions(float source, object expected)
    {
        var actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1234L, 1234.0)]
    [InlineData(12.5f, 12.5)]
    public void ToDecimalConversions(object source, object expected)
    {
        var actual = ValueConverter.ConvertTo(source, typeof(decimal));

        actual.ShouldBeOfType(typeof(decimal));
        actual.ShouldBe(new decimal((double)expected));
    }

    [Theory]
    [InlineData("100", "100")]
    [InlineData("100", 100)]
    [InlineData("100", (long)100)]
    [InlineData("100.1", 100.1f)]
    [InlineData("100.1", 100.1d)]
    public void StringConversions(string source, object expected)
    {
        var actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [Fact]
    public void StringConversionToDecimal()
    {
        string source = "100.1";
        decimal expected = 100.1m;
        var actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [Fact]
    public void StringConversionToDateTime()
    {
        var utcNow = DateTime.UtcNow;
        string source = utcNow.ToString("O", DateTimeFormatInfo.InvariantInfo);
        DateTime expected = utcNow;
        var actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

#if NET6_0_OR_GREATER

    [Fact]
    public void StringConversionToDateOnly()
    {
        var date = new DateOnly(2000, 10, 10);
        string source = date.ToString("O", DateTimeFormatInfo.InvariantInfo);
        var actual = ValueConverter.ConvertTo(source, typeof(DateOnly));

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType<DateOnly>();
        actual.ShouldBe(date);
    }

    [Fact]
    public void StringConversionToTimeOnly()
    {
        var time = new TimeOnly(1, 10, 10);
        string source = time.ToString("O", DateTimeFormatInfo.InvariantInfo);
        var actual = ValueConverter.ConvertTo(source, typeof(TimeOnly));

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType<TimeOnly>();
        actual.ShouldBe(time);
    }

#endif

    [Fact]
    public void StringConversionToDateTimeOffset()
    {
        var utcNow = DateTimeOffset.UtcNow;
        string source = utcNow.ToString("O", DateTimeFormatInfo.InvariantInfo);
        DateTimeOffset expected = utcNow;
        var actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }
}

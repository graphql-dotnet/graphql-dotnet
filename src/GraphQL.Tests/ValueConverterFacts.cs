using System.Globalization;
using System.Numerics;

namespace GraphQL.Tests;

public class ValueConverterFacts
{
    [Theory]
    [InlineData(1234L, 1234.0)]
    public void LongConversions(long source, object expected)
    {
        object? actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(12.5f, 12.5)]
    public void FloatConversions(float source, object expected)
    {
        object? actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1234L, 1234.0)]
    [InlineData(12.5f, 12.5)]
    public void ToDecimalConversions(object source, object expected)
    {
        object? actual = ValueConverter.ConvertTo(source, typeof(decimal));

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
        object? actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [Fact]
    public void StringConversionToDecimal()
    {
        const string source = "100.1";
        const decimal expected = 100.1m;
        object? actual = ValueConverter.ConvertTo(source, expected.GetType());

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
        object? actual = ValueConverter.ConvertTo(source, expected.GetType());

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
        object? actual = ValueConverter.ConvertTo(source, expected.GetType());

        actual.ShouldNotBeNull();
        actual.ShouldBeOfType(expected.GetType());
        actual.ShouldBe(expected);
    }

    [TheoryEx]
    [InlineData(typeof(string), typeof(DateTime))]
    [InlineData(typeof(string), typeof(sbyte))]
    [InlineData(typeof(string), typeof(byte))]
    [InlineData(typeof(string), typeof(short))]
    [InlineData(typeof(string), typeof(ushort))]
    [InlineData(typeof(string), typeof(int))]
    [InlineData(typeof(string), typeof(uint))]
    [InlineData(typeof(string), typeof(long))]
    [InlineData(typeof(string), typeof(ulong))]
    [InlineData(typeof(string), typeof(BigInteger))]
    [InlineData(typeof(string), typeof(float))]
    [InlineData(typeof(string), typeof(double))]
    [InlineData(typeof(string), typeof(decimal))]
#if NET6_0_OR_GREATER
    [InlineData(typeof(string), typeof(DateOnly))]
    [InlineData(typeof(string), typeof(TimeOnly))]
#endif
    [InlineData(typeof(string), typeof(DateTimeOffset))]
    [InlineData(typeof(string), typeof(bool))]
    [InlineData(typeof(string), typeof(Guid))]
    [InlineData(typeof(string), typeof(Uri))]
    [InlineData(typeof(string), typeof(byte[]))]
    [InlineData(typeof(DateTime), typeof(DateTimeOffset))]
    [InlineData(typeof(DateTimeOffset), typeof(DateTime))]
    [InlineData(typeof(TimeSpan), typeof(long))]
    [InlineData(typeof(int), typeof(sbyte))]
    [InlineData(typeof(int), typeof(byte))]
    [InlineData(typeof(int), typeof(short))]
    [InlineData(typeof(int), typeof(ushort))]
    [InlineData(typeof(int), typeof(bool))]
    [InlineData(typeof(int), typeof(uint))]
    [InlineData(typeof(int), typeof(long))]
    [InlineData(typeof(int), typeof(ulong))]
    [InlineData(typeof(int), typeof(BigInteger))]
    [InlineData(typeof(int), typeof(double))]
    [InlineData(typeof(int), typeof(float))]
    [InlineData(typeof(int), typeof(decimal))]
    [InlineData(typeof(int), typeof(TimeSpan))]
    [InlineData(typeof(long), typeof(sbyte))]
    [InlineData(typeof(long), typeof(byte))]
    [InlineData(typeof(long), typeof(short))]
    [InlineData(typeof(long), typeof(ushort))]
    [InlineData(typeof(long), typeof(int))]
    [InlineData(typeof(long), typeof(uint))]
    [InlineData(typeof(long), typeof(ulong))]
    [InlineData(typeof(long), typeof(BigInteger))]
    [InlineData(typeof(long), typeof(double))]
    [InlineData(typeof(long), typeof(float))]
    [InlineData(typeof(long), typeof(decimal))]
    [InlineData(typeof(long), typeof(TimeSpan))]
    [InlineData(typeof(BigInteger), typeof(sbyte))]
    [InlineData(typeof(BigInteger), typeof(byte))]
    [InlineData(typeof(BigInteger), typeof(decimal))]
    [InlineData(typeof(BigInteger), typeof(double))]
    [InlineData(typeof(BigInteger), typeof(short))]
    [InlineData(typeof(BigInteger), typeof(long))]
    [InlineData(typeof(BigInteger), typeof(ushort))]
    [InlineData(typeof(BigInteger), typeof(uint))]
    [InlineData(typeof(BigInteger), typeof(ulong))]
    [InlineData(typeof(BigInteger), typeof(int))]
    [InlineData(typeof(BigInteger), typeof(float))]
    [InlineData(typeof(uint), typeof(sbyte))]
    [InlineData(typeof(uint), typeof(byte))]
    [InlineData(typeof(uint), typeof(int))]
    [InlineData(typeof(uint), typeof(long))]
    [InlineData(typeof(uint), typeof(ulong))]
    [InlineData(typeof(uint), typeof(short))]
    [InlineData(typeof(uint), typeof(ushort))]
    [InlineData(typeof(uint), typeof(BigInteger))]
    [InlineData(typeof(ulong), typeof(BigInteger))]
    [InlineData(typeof(byte), typeof(sbyte))]
    [InlineData(typeof(byte), typeof(int))]
    [InlineData(typeof(byte), typeof(long))]
    [InlineData(typeof(byte), typeof(ulong))]
    [InlineData(typeof(byte), typeof(short))]
    [InlineData(typeof(byte), typeof(ushort))]
    [InlineData(typeof(byte), typeof(BigInteger))]
    [InlineData(typeof(sbyte), typeof(byte))]
    [InlineData(typeof(sbyte), typeof(int))]
    [InlineData(typeof(sbyte), typeof(long))]
    [InlineData(typeof(sbyte), typeof(ulong))]
    [InlineData(typeof(sbyte), typeof(short))]
    [InlineData(typeof(sbyte), typeof(ushort))]
    [InlineData(typeof(sbyte), typeof(BigInteger))]
    [InlineData(typeof(float), typeof(double))]
    [InlineData(typeof(float), typeof(decimal))]
    [InlineData(typeof(float), typeof(BigInteger))]
    [InlineData(typeof(double), typeof(float))]
    [InlineData(typeof(double), typeof(decimal))]
    [InlineData(typeof(char), typeof(byte))]
    [InlineData(typeof(char), typeof(int))]
    [InlineData(typeof(decimal), typeof(double))]
    public void ConverterExistence(Type valueType, Type targetType)
    {
        var actual = ValueConverter.GetConversion(valueType, targetType);

        actual.ShouldNotBeNull();
    }
}

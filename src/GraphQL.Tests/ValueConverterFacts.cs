using System;
using System.Globalization;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
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
}

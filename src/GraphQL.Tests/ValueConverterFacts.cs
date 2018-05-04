using System;
using System.Globalization;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class ValueConverterFacts
    {
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

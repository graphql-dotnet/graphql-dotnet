using GraphQL.Types;
using Shouldly;
using System;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ShortGraphTypeTests : ScalarGraphTypeTest<ShortGraphType>
    {
        [Fact]
        public void Coerces_given_inputs_to_null()
            => type.ParseValue(null).ShouldBe(null);

        [Theory]
        [InlineData(-999999)]
        [InlineData(999999)]
        [InlineData("999999")]
        [InlineData("-999999")]
        public void Coerces_given_inputs_to_overflow_exception(object input) =>
            AssertException<OverflowException>(input);

        [Theory]
        [InlineData("abc")]
        [InlineData("-999999.99")]
        public void Coerces_given_inputs_to_out_of_bound_exception(object input) =>
            AssertException<FormatException>(input);

        [Fact]
        public void Coerces_double_to_invalid_operation_exception()
            => AssertException<InvalidOperationException>(999999.99);

        [Theory]
        [InlineData("32767", 32767)]
        [InlineData(32767, 32767)]
        [InlineData("-32768", -32768)]
        [InlineData(-32768, -32768)]
        [InlineData("0", 0)]
        [InlineData(12, 12)]
        public void Coerces_given_inputs_to_short_value(object input, short expected)
            => type.ParseValue(input).ShouldBe(expected);
    }

    public class ScalarGraphTypeTest<T> where T : ScalarGraphType, new()
    {
        protected readonly T type = new T();

        protected void AssertException<T>(object value) where T : Exception =>
            Assert.Throws<T>(() => type.ParseValue(value));
    }
}

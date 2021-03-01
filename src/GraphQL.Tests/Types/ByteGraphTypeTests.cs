using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ByteGraphTypeTests : ScalarGraphTypeTest<ByteGraphType>
    {
        [Fact]
        public void Coerces_given_inputs_to_null()
            => type.ParseValue(null).ShouldBe(null);

        [Theory]
        [InlineData(-1)]
        public void Coerces_given_inputs_to_overflow_exception(object input) =>
            AssertException<OverflowException>(input);

        [Theory]
        [InlineData("256")]
        [InlineData("-1")]
        [InlineData("abc")]
        [InlineData("-999999.99")]
        [InlineData("12")]
        [InlineData("125")]
        public void Coerces_given_inputs_to_argument_exception(object input) =>
            AssertException<ArgumentException>(input);

        [Fact]
        public void Coerces_double_to_argument_exception()
            => AssertException<ArgumentException>(999999.99);

        [Theory]
        [InlineData(0, 0)]
        [InlineData(255, 255)]
        public void Coerces_input_to_valid_byte(object input, byte expected)
            => type.ParseValue(input).ShouldBe(expected);
    }
}

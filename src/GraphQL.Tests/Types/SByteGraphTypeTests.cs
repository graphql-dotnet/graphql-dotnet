using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class SByteGraphTypeTests : ScalarGraphTypeTest<SByteGraphType>
    {
        [Fact]
        public void Coerces_given_inputs_to_null()
            => type.ParseValue(null).ShouldBe(null);

        [Theory]
        [InlineData("128")]
        [InlineData(128)]
        [InlineData("-129")]
        [InlineData(-129)]
        public void Coerces_given_inputs_to_overflow_exception(object input) =>
            AssertException<OverflowException>(input);

        [Theory]
        [InlineData("abc")]
        [InlineData("-999999.99")]
        public void Coerces_given_inputs_to_out_of_bound_exception(object input) =>
            AssertException<FormatException>(input);

        [Fact]
        public void Coerces_double_to_invalid_operation_exception()
            => AssertException<InvalidOperationException>(999.99);

        [Theory]
        [InlineData("-12", -12)]
        [InlineData("-125", -125)]
        [InlineData(-128, -128)]
        [InlineData(127, 127)]
        public void Coerces_input_to_valid_sbyte(object input, sbyte expected)
            => type.ParseValue(input).ShouldBe(expected);
    }
}

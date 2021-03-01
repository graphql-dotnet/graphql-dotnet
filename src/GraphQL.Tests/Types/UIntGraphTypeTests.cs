using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class UIntGraphTypeTests : ScalarGraphTypeTest<UIntGraphType>
    {
        [Fact]
        public void Coerces_given_inputs_to_null()
            => type.ParseValue(null).ShouldBe(null);

        [Theory]
        [InlineData(-9223372036854775807)]
        [InlineData(-1)]
        [InlineData(9223372036854775807)]
        public void Coerces_given_inputs_to_overflow_exception(object input) =>
            AssertException<OverflowException>(input);

        [Theory]
        [InlineData("9223372036854775807")]
        [InlineData("-9223372036854775807")]
        [InlineData("-1")]
        [InlineData("abc")]
        [InlineData("-999999.99")]
        [InlineData("429496")]
        [InlineData("4294967295")]
        public void Coerces_given_inputs_to_out_of_bound_exception(object input) =>
            AssertException<ArgumentException>(input);

        [Fact]
        public void Coerces_double_to_invalid_operation_exception()
            => AssertException<ArgumentException>(999999.99);

        [Theory]
        [InlineData(0, 0)]
        [InlineData(65535, 65535)]
        public void Coerces_input_to_valid_uint(object input, uint expected)
            => type.ParseValue(input).ShouldBe(expected);
    }
}

using System;
using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ULongGraphTypeTests : ScalarGraphTypeTest<ULongGraphType>
    {
        [Fact]
        public void Coerces_given_inputs_to_null()
            => type.ParseValue(null).ShouldBe(null);

        [Theory]
        [InlineData(-1)]
        [InlineData(1E+55)]
        public void Coerces_given_inputs_to_overflow_exception(object input) =>
            AssertException<OverflowException>(input);

        [Theory]
        [InlineData("18446744073709551616")]
        [InlineData("-1")]
        [InlineData("abc")]
        [InlineData("-999999.99")]
        [InlineData("1844674407")]
        [InlineData("18446744073709551615")]
        public void Coerces_given_inputs_to_out_of_bound_exception(object input) =>
            AssertException<InvalidOperationException>(input);

        [Theory]
        [InlineData(0, 0)]
        [InlineData(65535, 65535)]
        [InlineData(65535.0, 65535)]
        public void Coerces_input_to_valid_ulong(object input, ulong expected)
            => type.ParseValue(input).ShouldBe(expected);

        [Fact]
        public void Reads_BigInt_Literals()
        {
            Assert.Equal(18446744073709551615ul, (ulong)type.ParseLiteral(new BigIntValue(System.Numerics.BigInteger.Parse("18446744073709551615"))));
        }
    }
}

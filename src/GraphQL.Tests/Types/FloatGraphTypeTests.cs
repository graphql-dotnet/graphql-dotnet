using System;
using System.Numerics;
using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class FloatGraphTypeTests
    {
        private readonly FloatGraphType type = new FloatGraphType();

        [Fact]
        public void coerces_null_to_null()
        {
            type.ParseValue(null).ShouldBeNull();
            type.ParseLiteral(new NullValue()).ShouldBeNull();
        }

        [Fact]
        public void coerces_invalid_string_to_exception()
        {
            Should.Throw<InvalidOperationException>(() => type.ParseValue("abcd"));
        }

        [Fact]
        public void coerces_double_to_value_using_cultures()
        {
            CultureTestHelper.UseCultures(coerces_double_to_value);
        }

        [Fact]
        public void coerces_double_to_value()
        {
            type.ParseValue(1.79769313486231e308d).ShouldBeOfType<double>().ShouldBe(1.79769313486231e308d);
            type.ParseLiteral(new FloatValue(1.79769313486231e308d)).ShouldBeOfType<double>().ShouldBe(1.79769313486231e308d);
        }

        [Fact]
        public void coerces_int_to_value()
        {
            type.ParseValue(1234567).ShouldBeOfType<double>().ShouldBe(1234567d);
            type.ParseLiteral(new IntValue(1234567)).ShouldBeOfType<double>().ShouldBe(1234567d);
        }

        [Fact]
        public void coerces_long_to_value()
        {
            type.ParseValue(12345678901234).ShouldBeOfType<double>().ShouldBe(12345678901234d);
            type.ParseLiteral(new LongValue(12345678901234)).ShouldBeOfType<double>().ShouldBe(12345678901234d);
        }

        [Fact]
        public void coerces_decimal_to_value()
        {
            type.ParseValue(9223372036854775808m).ShouldBeOfType<double>().ShouldBe(9223372036854775808d);
            type.ParseLiteral(new DecimalValue(9223372036854775808m)).ShouldBeOfType<double>().ShouldBe(9223372036854775808d);
        }

        [Fact]
        public void coerces_bigint_to_value()
        {
            type.ParseValue(new BigInteger(9999999999)).ShouldBeOfType<double>().ShouldBe(9999999999d);
            type.ParseLiteral(new BigIntValue(9999999999)).ShouldBeOfType<double>().ShouldBe(9999999999d);
        }
    }
}

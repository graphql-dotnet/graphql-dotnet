using System;
using System.Globalization;
using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class DecimalGraphTypeTests
    {
        private readonly DecimalGraphType _type = new DecimalGraphType();

        [Fact]
        public void coerces_null_to_null()
        {
            _type.ParseValue(null).ShouldBe(null);
        }

        [Fact]
        public void coerces_integer_to_decimal()
        {
            _type.ParseValue(0).ShouldBe((decimal) 0);
        }

        [Fact]
        public void coerces_invalid_string_to_exception()
        {
            Assert.Throws<FormatException>(()=>_type.ParseValue("abcd"));
        }

        [Fact]
        public void coerces_numeric_string_to_decimal_using_cultures()
        {
            CultureTestHelper.UseCultures(coerces_numeric_string_to_decimal);
        }

        [Fact]
        public void coerces_numeric_string_to_decimal()
        {
            _type.ParseValue("12345.6579").ShouldBe((decimal)12345.6579);
        }

        [Fact]
        public void converts_DecimalValue_to_decimal_using_cultures()
        {
            CultureTestHelper.UseCultures(converts_DecimalValue_to_decimal);
        }

        [Fact]
        public void converts_DecimalValue_to_decimal()
        {
            _type.ParseLiteral(new DecimalValue(12345.6579m)).ShouldBe((decimal)12345.6579);
        }
    }
}

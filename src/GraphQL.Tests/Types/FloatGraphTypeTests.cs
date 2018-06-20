using System;
using System.Globalization;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class FloatGraphTypeTests
    {
        private FloatGraphType type = new FloatGraphType();

        [Fact]
        public void coerces_null_to_null()
        {
            type.ParseValue(null).ShouldBe(null);
        }

        [Fact]
        public void coerces_invalid_string_to_exception()
        {
            Assert.Throws<FormatException>(()=>type.ParseValue("abcd"));
        }

        [Fact]
        public void coerces_double_to_value_using_cultures()
        {
            CultureTestHelper.UseCultures(coerces_double_to_value);
        }

        [Fact]
        public void coerces_double_to_value()
        {
            type.ParseValue(1.79769313486231e308).ShouldBe((double)1.79769313486231e308);
        }
    }
}

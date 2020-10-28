using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class BooleanGraphTypeTests
    {
        private readonly BooleanGraphType type = new BooleanGraphType();

        [Fact]
        public void coerces_0_to_false()
        {
            type.ParseValue(0).ShouldBe(false);
        }

        [Fact]
        public void coerces_1_to_true()
        {
            type.ParseValue(1).ShouldBe(true);
        }

        [Fact]
        public void coerces_string_false()
        {
            type.ParseValue("false").ShouldBe(false);
        }

        [Fact]
        public void coerces_string_False()
        {
            type.ParseValue("False").ShouldBe(false);
        }

        [Fact]
        public void coerces_string_true()
        {
            type.ParseValue("true").ShouldBe(true);
        }

        [Fact]
        public void coerces_string_True()
        {
            type.ParseValue("True").ShouldBe(true);
        }

        [Fact]
        public void coerces_string_1_to_true()
        {
            type.ParseValue("1").ShouldBe(true);
        }

        [Fact]
        public void coerces_zero_string_to_false()
        {
            type.ParseValue("0").ShouldBe(false);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("21")]
        public void coerces_input_to_exception(string input)
        {
            var formatException = Should.Throw<FormatException>(() => type.ParseValue(input));
            var formatException2 = Should.Throw<FormatException>(() => bool.Parse(input));
            formatException.Message.ShouldBe(formatException2.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData("0")]
        [InlineData("False")]
        [InlineData("false")]
        [InlineData(false)]
        public void serialize_input_to_false(object input)
        {
            type.Serialize(input).ShouldBe(false);
        }

        [Theory]
        [InlineData(1)]
        [InlineData("1")]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData(true)]
        public void serialize_input_to_true(object input)
        {
            type.Serialize(input).ShouldBe(true);
        }
    }
}

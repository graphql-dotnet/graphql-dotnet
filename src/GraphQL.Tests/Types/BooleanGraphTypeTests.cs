using GraphQL.Types;
using Shouldly;
using System;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class BooleanGraphTypeTests
    {
        private BooleanGraphType type = new BooleanGraphType();

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
            FormatException formatException = Should.Throw<FormatException>(() => type.ParseValue(input));
#if NETCOREAPP3_0
            formatException.Message.ShouldBe($"String '{input}' was not recognized as a valid Boolean.");
#else
            formatException.Message.ShouldBe("String was not recognized as a valid Boolean.");
#endif
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

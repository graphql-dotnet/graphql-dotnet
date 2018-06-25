using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    using System;

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
        public void coerces_string_1() =>
            type.ParseValue("1").ShouldBe(true);


        [Fact]
        public void coerces_string_0() =>
            type.ParseValue("0").ShouldBe(false);

        [Fact]
        public void coerces_string_notRelevantToBooleanValue_ThrowsFormatException() =>
            Assert.Throws<FormatException>(() => type.ParseValue("bdsjkfbdsk"));

        [Theory]
        [InlineData(true)]
        [InlineData("1")]
        [InlineData(1)]
        public void Serialize_valueIsWrittenAsAValidTrueValue_ReturnTrue(object input) =>
            type.Serialize(input).ShouldBe(true);

        [Theory]
        [InlineData(false)]
        [InlineData("0")]
        [InlineData(0)]
        public void Serialize_valueIsWrittenAsAValidFalseValue_ReturnFalse(object input) =>
            type.Serialize(input).ShouldBe(false);

        [Fact]
        public void Serialize_valueIsInvalid_ThrowsFormatException() =>
            Assert.Throws<FormatException>(() => type.Serialize("fdsfsd"));
    }
}

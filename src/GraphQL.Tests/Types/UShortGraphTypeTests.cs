using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class UShortGraphTypeTests : ScalarGraphTypeTest<UShortGraphType>
{
    [Fact]
    public void Coerces_given_inputs_to_null()
        => type.ParseValue(null).ShouldBe(null);

    [Theory]
    [InlineData(-999999)]
    [InlineData(-1)]
    [InlineData(999999)]
    public void Coerces_given_inputs_to_overflow_exception(object input) =>
        AssertException<OverflowException>(input);

    [Theory]
    [InlineData("999999")]
    [InlineData("-999999")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("-999999.99")]
    [InlineData("123")]
    [InlineData("65535")]
    [InlineData(999999.0)]
    [InlineData(65535.0)]
    public void Coerces_given_inputs_to_invalid_operation_exception(object input) =>
        AssertException<InvalidOperationException>(input);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(65535, 65535)]
    public void Coerces_input_to_valid_ushort(object input, ushort expected)
        => type.ParseValue(input).ShouldBe(expected);
}

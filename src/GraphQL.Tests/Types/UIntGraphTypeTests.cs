using System.Collections;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

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
    [InlineData(1E+55)]
    [InlineData(999999.0)]
    public void Coerces_given_inputs_to_invalid_operation_exception(object input) =>
        AssertException<InvalidOperationException>(input);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(65535, 65535)]
    public void Coerces_input_to_valid_uint(object input, uint expected)
        => type.ParseValue(input).ShouldBe(expected);

    [Theory]
    [MemberData(nameof(SerializeListData))]
    public void serialize_list(IEnumerable list, bool canSerializeNullableList, bool canSerializeNonNullList)
    {
        type.CanSerializeList(list, true).ShouldBe(canSerializeNullableList);
        type.CanSerializeList(list, false).ShouldBe(canSerializeNonNullList);
        type.SerializeList(list).ShouldBe(list);
    }

    public static object?[][] SerializeListData = [
        [new List<uint> { 1, 2 }, true, true],
        [new List<uint?> { 1, 2 }, true, true],
        [new List<uint?> { 1, 2, null }, true, false],
        [new uint[] { 1, 2 }, true, true],
        [new uint?[] { 1, 2 }, true, true],
        [new uint?[] { 1, 2, null }, true, false],
        [new int[] { 1, 2 }, false, false],
    ];
}

using System.Collections;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class ByteGraphTypeTests : ScalarGraphTypeTest<ByteGraphType>
{
    [Fact]
    public void Coerces_given_inputs_to_null()
        => type.ParseValue(null).ShouldBe(null);

    [Theory]
    [InlineData(-1)]
    [InlineData(3335)]
    public void Coerces_given_inputs_to_overflow_exception(object input) =>
        AssertException<OverflowException>(input);

    [Theory]
    [InlineData("256")]
    [InlineData(123.0)]
    [InlineData(333.0)]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("-999999.99")]
    [InlineData("12")]
    [InlineData("125")]
    public void Coerces_given_inputs_to_invalid_operation_exception(object input) =>
        AssertException<InvalidOperationException>(input);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(255, 255)]
    public void Coerces_input_to_valid_byte(object input, byte expected)
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
        [new List<byte> { 1, 2 }, true, true],
        [new List<byte?> { 1, 2 }, true, true],
        [new List<byte?> { 1, 2, null }, true, false],
        [new byte[] { 1, 2 }, true, true],
        [new byte?[] { 1, 2 }, true, true],
        [new byte?[] { 1, 2, null }, true, false],
        [new sbyte[] { 1, 2 }, false, false],
    ];
}

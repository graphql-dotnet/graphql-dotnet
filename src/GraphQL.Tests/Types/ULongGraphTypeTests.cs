using System.Collections;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

public class ULongGraphTypeTests : ScalarGraphTypeTest<ULongGraphType>
{
    [Fact]
    public void Coerces_given_inputs_to_null()
        => type.ParseValue(null).ShouldBe(null);

    [Theory]
    [InlineData(-1)]
    public void Coerces_given_inputs_to_overflow_exception(object input) =>
        AssertException<OverflowException>(input);

    [Theory]
    [InlineData("18446744073709551616")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("-999999.99")]
    [InlineData("1844674407")]
    [InlineData("18446744073709551615")]
    [InlineData(1E+55)]
    [InlineData(65535.0)]
    public void Coerces_given_inputs_to_invalid_operation_exception(object input) =>
        AssertException<InvalidOperationException>(input);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(65535, 65535)]
    public void Coerces_input_to_valid_ulong(object input, ulong expected)
        => type.ParseValue(input).ShouldBe(expected);

    [Fact]
    public void Reads_BigInt_Literals()
    {
        Assert.Equal(18446744073709551615ul, (ulong)type.ParseLiteral(new GraphQLIntValue(System.Numerics.BigInteger.Parse("18446744073709551615")))!);
    }

    [Theory]
    [MemberData(nameof(SerializeListData))]
    public void serialize_list(IEnumerable list, bool canSerializeNullableList, bool canSerializeNonNullList)
    {
        type.CanSerializeList(list, true).ShouldBe(canSerializeNullableList);
        type.CanSerializeList(list, false).ShouldBe(canSerializeNonNullList);
        type.SerializeList(list).ShouldBe(list);
    }

    public static object?[][] SerializeListData = [
        [new List<ulong> { 1, 2 }, true, true],
        [new List<ulong?> { 1, 2 }, true, true],
        [new List<ulong?> { 1, 2, null }, true, false],
        [new ulong[] { 1, 2 }, true, true],
        [new ulong?[] { 1, 2 }, true, true],
        [new ulong?[] { 1, 2, null }, true, false],
        [new long[] { 1, 2 }, false, false],
    ];
}

using System.Collections;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class BooleanGraphTypeTests
{
    private readonly BooleanGraphType type = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("False")]
    [InlineData("True")]
    [InlineData("abc")]
    [InlineData("21")]
    public void parse_throws(object value)
    {
        Should.Throw<InvalidOperationException>(() => type.ParseValue(value));
        type.CanParseValue(value).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData("false")]
    [InlineData("true")]
    [InlineData("False")]
    [InlineData("True")]
    [InlineData("abc")]
    [InlineData("21")]
    public void serialize_throws(object value)
    {
        Should.Throw<InvalidOperationException>(() => type.Serialize(value));
    }

    [Fact]
    public void serialize_input_to_false()
    {
        type.Serialize(false).ShouldBe(false);
    }

    [Fact]
    public void serialize_input_to_true()
    {
        type.Serialize(true).ShouldBe(true);
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
        [new List<bool> { true, false }, true, true],
        [new List<bool?> { true, false }, true, true],
        [new List<bool?> { true, false, null }, true, false],
    ];
}

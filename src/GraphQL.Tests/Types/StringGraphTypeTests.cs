using System.Collections;
using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class StringGraphTypeTests
{
    private readonly StringGraphType _type;

    public StringGraphTypeTests()
    {
        _type = new StringGraphType();
    }

    [Fact]
    public void serialize_keeps_quotes()
    {
        _type.Serialize("\"one\"").ShouldBe("\"one\"");
    }

    [Fact]
    public void serializes_int_to_string_throws()
    {
        Should.Throw<InvalidOperationException>(() => _type.Serialize(1));
    }

    [Fact]
    public void serializes_long_to_string_throws()
    {
        Should.Throw<InvalidOperationException>(() => _type.Serialize(long.MaxValue));
    }

    [Fact]
    public void serializes_null_to_null()
    {
        _type.Serialize(null).ShouldBeNull();
    }

    [Fact]
    public void parse_value_keeps__quotes()
    {
        _type.ParseValue("\"one\"").ShouldBe("\"one\"");
    }

    [Fact]
    public void keeps_quotes_in_string()
    {
        _type.ParseValue("one \" two").ShouldBe("one \" two");
    }

    [Fact]
    public void keeps_single_quote()
    {
        _type.ParseValue("\"").ShouldBe("\"");
    }

    [Theory]
    [MemberData(nameof(SerializeListData))]
    public void serialize_list(IEnumerable list, bool canSerializeNullableList, bool canSerializeNonNullList)
    {
        _type.CanSerializeList(list, true).ShouldBe(canSerializeNullableList);
        _type.CanSerializeList(list, false).ShouldBe(canSerializeNonNullList);
        _type.SerializeList(list).ShouldBe(list);
    }

    public static object?[][] SerializeListData = [
        [new List<string> { "1", "2" }, true, true],
        [new List<string?> { "1", "2" }, true, true],
        [new List<string?> { "1", "2", null }, true, false],
        [new string[] { "1", "2" }, true, true],
        [new string?[] { "1", "2" }, true, true],
        [new string?[] { "1", "2", null }, true, false],
    ];
}

using System.Collections;
using System.Numerics;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

public class IdGraphTypeTests
{
    private readonly IdGraphType _type = new();

    [Fact]
    public void parse_literal_null_returns_null()
    {
        _type.ParseLiteral(new GraphQLNullValue()).ShouldBeNull();
    }

    [Fact]
    public void parse_value_null_returns_null()
    {
        _type.ParseValue(null).ShouldBeNull();
    }

    [Fact]
    public void serialize_null_returns_null()
    {
        _type.Serialize(null).ShouldBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000000000L)]
    [InlineData("hello")]
    public void parse_literal_value_to_identifier(object value)
    {
        GraphQLValue? ast = value switch
        {
            int i => new GraphQLIntValue(i),
            long l => new GraphQLIntValue(l),
            string s => new GraphQLStringValue(s),
            _ => null
        };
        object? ret = _type.ParseLiteral(ast!);
        ret.ShouldBeOfType(value.GetType());
        ret.ShouldBe(value);
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((sbyte)2)]
    [InlineData((short)3)]
    [InlineData((ushort)4)]
    [InlineData((int)5)]
    [InlineData((uint)6)]
    [InlineData((long)7)]
    [InlineData((ulong)8)]
    [InlineData("hello")]
    public void parse_value_to_identifier(object value)
    {
        object? ret = _type.ParseValue(value);
        ret.ShouldBeOfType(value.GetType());
        ret.ShouldBe(value);
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((sbyte)2)]
    [InlineData((short)3)]
    [InlineData((ushort)4)]
    [InlineData((int)5)]
    [InlineData((uint)6)]
    [InlineData((long)7)]
    [InlineData((ulong)8)]
    [InlineData("hello")]
    public void serialize_value(object value)
    {
        object? ret = _type.Serialize(value);
        ret.ShouldBeOfType(typeof(string));
        ret.ShouldBe(value.ToString());
    }

    [Fact]
    public void boolean_literal_throws()
    {
        Should.Throw<InvalidOperationException>(() => _type.ParseLiteral(new GraphQLTrueBooleanValue()));
    }

    [Fact]
    public void boolean_value_throws()
    {
        Should.Throw<InvalidOperationException>(() => _type.ParseValue(true));
    }

    [Fact]
    public void serialize_boolean_throws()
    {
        Should.Throw<InvalidOperationException>(() => _type.Serialize(true));
    }

    [Fact]
    public void parse_guid_value()
    {
        var g = new Guid("12345678901234567890123456789012");
        object? ret = _type.ParseValue(g);
        ret.ShouldBeOfType(typeof(Guid));
        ret.ShouldBe(g);
    }

    [Fact]
    public void serialize_guid()
    {
        var g = new Guid("12345678901234567890123456789012");
        object? ret = _type.Serialize(g);
        ret.ShouldBeOfType(typeof(string));
        ret.ShouldBe(g.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [MemberData(nameof(SerializeListData))]
    public void serialize_int_list(IEnumerable list, bool canSerializeNullableList, bool canSerializeNonNullList, IEnumerable<string> expected)
    {
        _type.CanSerializeList(list, true).ShouldBe(canSerializeNullableList);
        _type.CanSerializeList(list, false).ShouldBe(canSerializeNonNullList);
        if (canSerializeNonNullList || canSerializeNullableList)
        {
            var ret = _type.SerializeList(list);
            var actual = ret.ShouldBeAssignableTo<IEnumerable<string>>();
            actual.ShouldBe(expected);
        }
    }

    public static object?[][] SerializeListData = [
        // Int data type test cases
        [new List<int> { 1, 2, 3 }, true, true, new string[] { "1", "2", "3" }],
        [new List<int?> { 1, 2, 3 }, true, true, new string[] { "1", "2", "3" }],
        [new List<int?> { 1, 2, null }, true, false, new string?[] { "1", "2", null }],
        [new int[] { 1, 2, 3 }, true, true, new string[] { "1", "2", "3" }],
        [new int?[] { 1, 2, 3 }, true, true, new string[] { "1", "2", "3" }],
        [new int?[] { 1, 2, null }, true, false, new string?[] { "1", "2", null }],
        [new uint[] { 1, 2, 3 }, false, false, null],
        [new List<uint> { 1, 2, 3 }, false, false, null],

        // Long data type test cases
        [new List<long> { 1L, 2L, 3L }, true, true, new string[] { "1", "2", "3" }],
        [new List<long?> { 1L, 2L, 3L }, true, true, new string[] { "1", "2", "3" }],
        [new List<long?> { 1L, 2L, null }, true, false, new string?[] { "1", "2", null }],
        [new long[] { 1L, 2L, 3L }, true, true, new string[] { "1", "2", "3" }],
        [new long?[] { 1L, 2L, 3L }, true, true, new string[] { "1", "2", "3" }],
        [new long?[] { 1L, 2L, null }, true, false, new string?[] { "1", "2", null }],
        [new ulong[] { 1, 2, 3 }, false, false, null],
        [new List<ulong> { 1, 2, 3 }, false, false, null],

        // Guid data type test cases
        [new List<Guid> { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333") }, true, true, new string[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "33333333-3333-3333-3333-333333333333" }],
        [new List<Guid?> { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), null }, true, false, new string?[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", null }],
        [new Guid[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333") }, true, true, new string[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "33333333-3333-3333-3333-333333333333" }],
        [new Guid?[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), null }, true, false, new string?[] { "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", null }],

        // String data type test cases
        [new List<string> { "a", "b", "c" }, true, true, new string[] { "a", "b", "c" }],
        [new List<string?> { "a", "b", null }, true, false, new string?[] { "a", "b", null }],
        [new string[] { "a", "b", "c" }, true, true, new string[] { "a", "b", "c" }],
        [new string?[] { "a", "b", null }, true, false, new string?[] { "a", "b", null }],

        // Other data types
        [new byte[] { 1, 2, 3 }, false, false, null],
        [new sbyte[] { 1, 2, 3 }, false, false, null],
        [new short[] { 1, 2, 3 }, false, false, null],
        [new ushort[] { 1, 2, 3 }, false, false, null],
        [new BigInteger[] { 1, 2, 3 }, false, false, null],
    ];

}

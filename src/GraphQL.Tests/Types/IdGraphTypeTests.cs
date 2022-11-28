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
        GraphQLValue ast = value switch
        {
            int i => new GraphQLIntValue(i),
            long l => new GraphQLIntValue(l),
            string s => new GraphQLStringValue(s),
            _ => null
        };
        var ret = _type.ParseLiteral(ast);
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
        var ret = _type.ParseValue(value);
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
        var ret = _type.Serialize(value);
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
        var ret = _type.ParseValue(g);
        ret.ShouldBeOfType(typeof(Guid));
        ret.ShouldBe(g);
    }

    [Fact]
    public void serialize_guid()
    {
        var g = new Guid("12345678901234567890123456789012");
        var ret = _type.Serialize(g);
        ret.ShouldBeOfType(typeof(string));
        ret.ShouldBe(g.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
    }
}

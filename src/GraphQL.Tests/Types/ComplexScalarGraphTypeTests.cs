#nullable enable

using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

public class ComplexScalarGraphTypeTests
{
    private readonly ComplexScalarGraphType type = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(false)]
    [InlineData(true)]
    [InlineData(1L)]
    [InlineData(1.6f)]
    [InlineData(1.6)]
    [InlineData("test")]
    [InlineData(null)]
    public void parses_and_serializes(object? value)
    {
        type.CanParseValue(value).ShouldBeTrue();
        type.ParseValue(value).ShouldBe(value);
        type.Serialize(value).ShouldBe(value);
    }

    [Fact]
    public void parses_literals()
    {
        var values = new (GraphQLValue, object?)[] {
            (new GraphQLNullValue(), null),
            (new GraphQLIntValue(234), 234),
            (new GraphQLFloatValue(2.33), 2.33),
            (new GraphQLStringValue("test"), "test"),
        };

        foreach (var value in values)
        {
            type.CanParseLiteral(value.Item1).ShouldBeTrue();
            type.ParseLiteral(value.Item1).ShouldBe(value.Item2);
            type.ToAST(value.Item2).GetType().ShouldBe(value.Item1.GetType());
            type.ParseLiteral(type.ToAST(value.Item2)).ShouldBe(value.Item2);
        }
    }

    [Fact]
    public void handle_complex_types()
    {
        var obj = new Dictionary<string, object?>()
        {
            { "a", 1 },
            { "b", 2.5 },
            { "c", new object?[] {3, null, 4.2, "testing"} },
            { "d", null }
        };
        type.CanParseValue(obj).ShouldBeTrue();
        type.ParseValue(obj).ShouldBe(obj);
        var ast = type.ToAST(obj);
        type.CanParseLiteral(ast).ShouldBeTrue();
        var parsed = type.ParseLiteral(ast);
        System.Text.Json.JsonSerializer.Serialize(obj)
            .ShouldBe(System.Text.Json.JsonSerializer.Serialize(parsed));
    }
}

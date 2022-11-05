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
}

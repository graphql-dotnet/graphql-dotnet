using GraphQL.Execution;

namespace GraphQL.Tests.Bugs;

public class Bug256NullableEnumTests
{
    public enum EnumType
    {
        A,
        B
    }

    [Fact]
    public void nullable_enum_returns_value()
    {
        var ctx = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue> { { "value", new ArgumentValue(EnumType.B, ArgumentSource.Literal) } }
        };

        var result = ctx.GetArgument<EnumType?>("value");
        result.ShouldBe(EnumType.B);
    }

    [Fact]
    public void nullable_enum_returns_null()
    {
        var ctx = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue> { { "value", ArgumentValue.NullLiteral } }
        };

        var result = ctx.GetArgument<EnumType?>("value");
        result.ShouldBeNull();
    }

    [Fact]
    public void null_enum_returns_default()
    {
        var ctx = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue> { { "value", ArgumentValue.NullLiteral } }
        };

        var result = ctx.GetArgument<EnumType>("value");
        result.ShouldBe(EnumType.A);

        // just place it here to also check for default value
        var result2 = ctx.GetArgument<int>("value");
        result2.ShouldBe(0);
    }

    [Fact]
    public void enum_returns_value()
    {
        var ctx = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue> { { "value", new ArgumentValue(EnumType.B, ArgumentSource.Literal) } }
        };

        var result = ctx.GetArgument<EnumType>("value");
        result.ShouldBe(EnumType.B);
    }
}

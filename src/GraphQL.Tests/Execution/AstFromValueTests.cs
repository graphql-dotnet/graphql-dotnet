using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Execution;

public class AstFromValueTests
{
    [Fact]
    public void throws_for_null_graphtype()
    {
        Should.Throw<ArgumentNullException>(() => ((IGraphType)null!).ToAST(true));
    }

    [Fact]
    public void converts_null_to_null()
    {
        object? value = null;
        var result = new StringGraphType().ToAST(value);
        result.ShouldBeOfType<GraphQLNullValue>();
    }

    [Fact]
    public void converts_string_to_string_value()
    {
        var result = new StringGraphType().ToAST("test");
        result.ShouldBeOfType<GraphQLStringValue>().Value.ShouldBe("test");
    }

    [Fact]
    public void converts_bool_to_boolean_value()
    {
        var result = new BooleanGraphType().ToAST(true);
        result.ShouldBeAssignableTo<GraphQLBooleanValue>().ShouldNotBeNull().Value.ShouldBe("true");
    }

    [Fact]
    public void converts_long_to_long_value()
    {
        const long val = 12345678910111213;
        var result = new LongGraphType().ToAST(val);
        result.ShouldBeOfType<GraphQLIntValue>().Value.ShouldBe("12345678910111213");
    }

    [Fact]
    public void converts_long_to_int_value()
    {
        const long val = 12345678910111213;
        Should.Throw<OverflowException>(() => new IntGraphType().ToAST(val));
    }

    [Fact]
    public void converts_decimal_to_decimal_value()
    {
        const decimal val = 1234.56789m;
        var result = new DecimalGraphType().ToAST(val);
        result.ShouldBeOfType<GraphQLFloatValue>().Value.ShouldBe("1234.56789");
    }

    [Fact]
    public void converts_int_to_int_value()
    {
        const int val = 123;
        var result = new IntGraphType().ToAST(val);
        result.ShouldBeOfType<GraphQLIntValue>().Value.ShouldBe("123");
    }

    [Fact]
    public void converts_double_to_float_value()
    {
        const double val = 0.42;
        var result = new FloatGraphType().ToAST(val);
        result.ShouldBeOfType<GraphQLFloatValue>().Value.ShouldBe("0.42");
    }

    [Fact]
    public void converts_byte_to_int_value()
    {
        const byte value = 12;
        var result = new ByteGraphType().ToAST(value);
        result.ShouldBeOfType<GraphQLIntValue>().Value.ShouldBe("12");
    }

    [Fact]
    public void converts_sbyte_to_int_value()
    {
        const sbyte val = -12;
        var result = new SByteGraphType().ToAST(val);
        result.ShouldBeOfType<GraphQLIntValue>().Value.ShouldBe("-12");
    }

    [Fact]
    public void converts_uri_to_string_value()
    {
        var val = new Uri("http://www.wp.pl");
        var result = new UriGraphType().ToAST(val);
        result.ShouldBeOfType<GraphQLStringValue>().Value.ShouldBe(val.ToString());
    }
}

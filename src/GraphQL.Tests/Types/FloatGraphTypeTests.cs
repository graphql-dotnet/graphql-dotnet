using System.Numerics;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class FloatGraphTypeTests
{
    private readonly FloatGraphType type = new();

    [Fact]
    public void coerces_null_to_null()
    {
        type.ParseValue(null).ShouldBeNull();
        type.ParseLiteral(new GraphQLNullValue()).ShouldBeNull();
    }

    [Fact]
    public void coerces_invalid_string_to_exception()
    {
        Should.Throw<InvalidOperationException>(() => type.ParseValue("abcd"));
    }

    [Fact]
    public void coerces_double_to_value_using_cultures()
    {
        CultureTestHelper.UseCultures(coerces_double_to_value);
    }

    [Fact]
    public void coerces_double_to_value()
    {
        type.ParseValue(1.79769313486231e308d).ShouldBeOfType<double>().ShouldBe(1.79769313486231e308d);
        type.ParseLiteral(new GraphQLFloatValue(1.79769313486231e308d)).ShouldBeOfType<double>().ShouldBe(1.79769313486231e308d);
    }

    [Fact]
    public void coerces_int_to_value()
    {
        type.ParseValue(1234567).ShouldBeOfType<double>().ShouldBe(1234567d);
        type.ParseLiteral(new GraphQLIntValue(1234567)).ShouldBeOfType<double>().ShouldBe(1234567d);
    }

    [Fact]
    public void coerces_long_to_value()
    {
        type.ParseValue(12345678901234).ShouldBeOfType<double>().ShouldBe(12345678901234d);
        type.ParseLiteral(new GraphQLIntValue(12345678901234)).ShouldBeOfType<double>().ShouldBe(12345678901234d);
    }

    [Fact]
    public void coerces_decimal_to_value()
    {
        type.ParseValue(9223372036854775808m).ShouldBeOfType<double>().ShouldBe(9223372036854775808d);
        type.ParseLiteral(new GraphQLFloatValue(9223372036854775808m)).ShouldBeOfType<double>().ShouldBe(9223372036854775808d);
    }

    [Fact]
    public void coerces_bigint_to_value()
    {
        type.ParseValue(new BigInteger(9999999999)).ShouldBeOfType<double>().ShouldBe(9999999999d);
        type.ParseLiteral(new GraphQLIntValue(9999999999)).ShouldBeOfType<double>().ShouldBe(9999999999d);
    }
}

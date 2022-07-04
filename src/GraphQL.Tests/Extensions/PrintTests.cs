using System.Globalization;
using GraphQLParser.AST;

namespace GraphQL.Tests.Extensions;

[Collection("StaticTests")]
public class PrintTests
{
    [Fact]
    public void prints_int_value()
    {
        int value = 3;
        var val = new GraphQLIntValue(value);
        var result = val.Print();
        result.ShouldBe("3");
    }

    [Fact]
    public void prints_long_value()
    {
        long value = 3;
        var val = new GraphQLIntValue(value);
        var result = val.Print();
        result.ShouldBe("3");
    }

    [Fact]
    public void prints_float_value_using_cultures()
    {
        CultureTestHelper.UseCultures(prints_float_value);
    }

    [Fact]
    public void prints_float_value()
    {
        double value = 3.33;

        var val = new GraphQLFloatValue(value);
        var result = val.Print();
        result.ShouldBe(value.ToString("0.0##", NumberFormatInfo.InvariantInfo));
    }

    [Fact]
    public void string_encodes_control_characters()
    {
        var sample = new string(Enumerable.Range(0, 256).Select(x => (char)x).ToArray());
        var ret = new GraphQLStringValue(sample).Print();

        foreach (char c in ret)
            c.ShouldBeGreaterThanOrEqualTo(' ');

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<string>(ret);
        deserialized.ShouldBe(sample);

        var token = GraphQLParser.Lexer.Lex(ret);
        token.Kind.ShouldBe(GraphQLParser.TokenKind.STRING);
        token.Value.ShouldBe(sample);
    }

    [Theory]
    [MemberData(nameof(NodeTests))]
    public void prints_node(ASTNode node, string expected)
    {
        var printed = node.Print();

        printed.ShouldBe(expected);

        if (node is GraphQLStringValue str)
        {
            var token = GraphQLParser.Lexer.Lex(printed);
            token.Kind.ShouldBe(GraphQLParser.TokenKind.STRING);
            token.Value.ShouldBe(str.Value);
        }
    }

    [Theory]
    [MemberData(nameof(NodeTests))]
    public void prints_node_cultures(ASTNode node, string expected)
    {
        CultureTestHelper.UseCultures(() => prints_node(node, expected));
    }

    public static object[][] NodeTests = new object[][]
    {
        new object[] { new GraphQLStringValue("test"), @"""test""" },
        new object[] { new GraphQLStringValue("ab/cd"), @"""ab/cd""" },
        new object[] { new GraphQLStringValue("ab\bcd"), @"""ab\bcd""" },
        new object[] { new GraphQLStringValue("ab\fcd"), @"""ab\fcd""" },
        new object[] { new GraphQLStringValue("ab\rcd"), @"""ab\rcd""" },
        new object[] { new GraphQLStringValue("ab\ncd"), @"""ab\ncd""" },
        new object[] { new GraphQLStringValue("ab\tcd"), @"""ab\tcd""" },
        new object[] { new GraphQLStringValue("ab\\cd"), @"""ab\\cd""" },
        new object[] { new GraphQLStringValue("ab\"cd"), @"""ab\""cd""" },
        new object[] { new GraphQLStringValue("ab\u0019cd"), @"""ab\u0019cd""" },
        new object[] { new GraphQLStringValue("\"abcd\""), @"""\""abcd\""""" },
        new object[] { new GraphQLIntValue(int.MinValue), "-2147483648" },
        new object[] { new GraphQLIntValue(0), "0" },
        new object[] { new GraphQLIntValue(int.MaxValue), "2147483647" },
        new object[] { new GraphQLIntValue(long.MinValue), "-9223372036854775808" },
        new object[] { new GraphQLIntValue(0), "0" },
        new object[] { new GraphQLIntValue(long.MaxValue), "9223372036854775807" },
        new object[] { new GraphQLIntValue(new System.Numerics.BigInteger(decimal.MaxValue) * 1000 + 2), "79228162514264337593543950335002" },
        new object[] { new GraphQLFloatValue(double.MinValue), "-1.79769313486232E+308" },
        new object[] { new GraphQLFloatValue(double.Epsilon), "4.94065645841247E-324" },
        new object[] { new GraphQLFloatValue(double.MaxValue), "1.79769313486232E+308" },
        new object[] { new GraphQLFloatValue(0.00000001256), "1.256E-08" },
        new object[] { new GraphQLFloatValue(12.56), "12.56" },
        new object[] { new GraphQLFloatValue(3.33), "3.33" },
        new object[] { new GraphQLFloatValue(1.0), "1" }, // double
        new object[] { new GraphQLFloatValue((float)1.0), "1" },
        new object[] { new GraphQLFloatValue((float)34), "34" },
        new object[] { new GraphQLFloatValue(1.0000m), "1.0000" },
        new object[] { new GraphQLFloatValue(1.0m), "1.0" },
        new object[] { new GraphQLFloatValue(1m), "1" },
        new object[] { new GraphQLFloatValue(decimal.MinValue), "-79228162514264337593543950335"},
        new object[] { new GraphQLFloatValue(decimal.MaxValue), "79228162514264337593543950335"},
        new object[] { new GraphQLFloatValue(0.00000000000000001), "1E-17"}, // double: G15 format
        new object[] { new GraphQLFloatValue(0.00000000000000001m), "0.00000000000000001"}, // decimal
        new object[] { new GraphQLFalseBooleanValue(), "false"},
        new object[] { new GraphQLTrueBooleanValue(), "true"},
        new object[] { new GraphQLEnumValue { Name = new GraphQLName("TEST") }, "TEST"},
    };
}

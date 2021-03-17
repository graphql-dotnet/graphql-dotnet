using System;
using System.Globalization;
using System.Linq;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Utilities;
using GraphQL.Utilities.Federation;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class AstPrinterTests
    {
        private readonly AstPrintVisitor _printer = new AstPrintVisitor();
        private readonly IDocumentBuilder _builder = new GraphQLDocumentBuilder();

        [Fact]
        public void prints_ast()
        {
            var query = @"{
  complicatedArgs {
    intArgField(intArg: 2)
  }
}
";
            var document = _builder.Build(query);

            var result = _printer.Visit(document);
            result.ShouldNotBeNull();
            result.ToString().ShouldBe(MonetizeLineBreaks(query));
        }

        [Fact]
        public void prints_variables()
        {
            var query = @"mutation createUser($userInput: UserInput!) {
  createUser(userInput: $userInput) {
    id
    gender
    profileImage
  }
}
";

            var document = _builder.Build(query);

            var result = _printer.Visit(document);
            result.ShouldNotBeNull();
            result.ToString().ShouldBe(MonetizeLineBreaks(query));
        }

        [Fact]
        public void prints_inline_fragments()
        {
            var query = @"query users {
  users {
    id
    union {
      ... on UserType {
        username
      }
      ... on CustomerType {
        customername
      }
    }
  }
}
";

            var document = _builder.Build(query);

            var result = _printer.Visit(document);
            result.ShouldNotBeNull();
            result.ToString().ShouldBe(MonetizeLineBreaks(query));
        }

        [Fact]
        public void prints_int_value()
        {
            int value = 3;
            var val = new IntValue(value);
            var result = _printer.Visit(val);
            result.ShouldBe("3");
        }

        [Fact]
        public void prints_long_value()
        {
            long value = 3;
            var val = new LongValue(value);
            var result = _printer.Visit(val);
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

            var val = new FloatValue(value);
            var result = _printer.Visit(val);
            result.ShouldBe(value.ToString("0.0##", NumberFormatInfo.InvariantInfo));
        }

        private static string MonetizeLineBreaks(string input)
        {
            return (input ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }

        [Fact]
        public void anynode_throws()
        {
            Should.Throw<InvalidOperationException>(() => AstPrinter.Print(new AnyValue("")));
        }

        [Fact]
        public void string_encodes_control_characters()
        {
            var sample = new string(Enumerable.Range(0, 256).Select(x => (char)x).ToArray());
            var ret = AstPrinter.Print(new StringValue(sample));

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
        public void prints_node(INode node, string expected)
        {
            var printed = AstPrinter.Print(node);

            printed.ShouldBe(expected);

            if (node is StringValue str)
            {
                var token = GraphQLParser.Lexer.Lex(printed);
                token.Kind.ShouldBe(GraphQLParser.TokenKind.STRING);
                token.Value.ShouldBe(str.Value);
            }
        }

        [Theory]
        [MemberData(nameof(NodeTests))]
        public void prints_node_cultures(INode node, string expected)
        {
            CultureTestHelper.UseCultures(() => prints_node(node, expected));
        }

        public static object[][] NodeTests = new object[][]
        {
            new object[] { new StringValue("test"), @"""test""" },
            new object[] { new StringValue("ab/cd"), @"""ab\/cd""" },
            new object[] { new StringValue("ab\bcd"), @"""ab\bcd""" },
            new object[] { new StringValue("ab\fcd"), @"""ab\fcd""" },
            new object[] { new StringValue("ab\rcd"), @"""ab\rcd""" },
            new object[] { new StringValue("ab\ncd"), @"""ab\ncd""" },
            new object[] { new StringValue("ab\tcd"), @"""ab\tcd""" },
            new object[] { new StringValue("ab\\cd"), @"""ab\\cd""" },
            new object[] { new StringValue("ab\"cd"), @"""ab\""cd""" },
            new object[] { new StringValue("ab\u0019cd"), @"""ab\u0019cd""" },
            new object[] { new StringValue("\"abcd\""), @"""\""abcd\""""" },
            new object[] { new IntValue(int.MinValue), "-2147483648" },
            new object[] { new IntValue(0), "0" },
            new object[] { new IntValue(int.MaxValue), "2147483647" },
            new object[] { new LongValue(long.MinValue), "-9223372036854775808" },
            new object[] { new LongValue(0), "0" },
            new object[] { new LongValue(long.MaxValue), "9223372036854775807" },
            new object[] { new BigIntValue(new System.Numerics.BigInteger(decimal.MaxValue) * 1000 + 2), "79228162514264337593543950335002" },
            new object[] { new FloatValue(double.MinValue), "-1.79769313486232E+308" },
            new object[] { new FloatValue(double.Epsilon), "4.94065645841247E-324" },
            new object[] { new FloatValue(double.MaxValue), "1.79769313486232E+308" },
            new object[] { new FloatValue(0.00000001256), "1.256E-08" },
            new object[] { new FloatValue(12.56), "12.56" },
            new object[] { new FloatValue(3.33), "3.33" },
            new object[] { new FloatValue(1.0), "1" },
            new object[] { new FloatValue(34), "34" },
            new object[] { new DecimalValue(1.0000m), "1"},
            new object[] { new DecimalValue(decimal.MinValue), "-79228162514264337593543950335"},
            new object[] { new DecimalValue(decimal.MaxValue), "79228162514264337593543950335"},
            new object[] { new DecimalValue(0.00000000000000001m), "1E-17"},
            new object[] { new BooleanValue(false), "false"},
            new object[] { new BooleanValue(true), "true"},
            new object[] { new EnumValue("TEST"), "TEST"},
        };
    }
}

using System.Globalization;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Utilities;
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
            result.ShouldBe(value);
        }

        [Fact]
        public void prints_long_value()
        {
            long value = 3;
            var val = new LongValue(value);
            var result = _printer.Visit(val);
            result.ShouldBe(value);
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
    }
}

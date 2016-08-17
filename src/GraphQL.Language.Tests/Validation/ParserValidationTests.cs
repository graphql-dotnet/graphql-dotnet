using System;
using Xunit;

namespace GraphQL.Language.Tests.Validation
{
    public class ParserValidationTests
    {
        [Fact]
        public void Parse_FragmentInvalidOnName_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("fragment on on on { on }")));

            Assert.Equal(@"Syntax Error GraphQL (1:10) Unexpected Name " + "\"on\"" + @"
1: fragment on on on { on }
            ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_InvalidDefaultValue_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("query Foo($x: Complex = { a: { b: [ $var ] } }) { field }")));

            Assert.Equal(@"Syntax Error GraphQL (1:37) Unexpected $
1: query Foo($x: Complex = { a: { b: [ $var ] } }) { field }
                                       ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_InvalidFragmentNameInSpread_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("{ ...on }")));

            Assert.Equal(@"Syntax Error GraphQL (1:9) Expected Name, found }
1: { ...on }
           ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_InvalidNullAsValue_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("{ fieldWithNullableStringInput(input: null) }")));

            Assert.Equal(@"Syntax Error GraphQL (1:39) Unexpected Name " + "\"null\"" + @"
1: { fieldWithNullableStringInput(input: null) }
                                         ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_LonelySpread_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("...")));

            Assert.Equal(@"Syntax Error GraphQL (1:1) Unexpected ...
1: ...
   ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_MissingEndingBrace_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("{")));

            Assert.Equal(@"Syntax Error GraphQL (1:2) Expected Name, found EOF
1: {
    ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_MissingFieldNameWhenAliasing_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("{ field: {} }")));

            Assert.Equal(@"Syntax Error GraphQL (1:10) Expected Name, found {
1: { field: {} }
            ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_MissingFragmentType_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source(@"{ ...MissingOn }
fragment MissingOn Type")));

            Assert.Equal(@"Syntax Error GraphQL (2:20) Expected " + "\"on\"" + @", found Name " + "\"Type\"" + @"
1: { ...MissingOn }
2: fragment MissingOn Type
                      ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Parse_UnknownOperation_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Parser(new Lexer()).Parse(new Source("notanoperation Foo { field }")));

            Assert.Equal(@"Syntax Error GraphQL (1:1) Unexpected Name " + "\"notanoperation\"" + @"
1: notanoperation Foo { field }
   ^
".Replace(Environment.NewLine, "\n"), exception.Message);
        }
    }
}

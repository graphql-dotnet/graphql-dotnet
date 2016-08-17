using System;
using Xunit;

namespace GraphQL.Language.Tests.Validation
{
    public class LexerValidationTests
    {
        [Fact]
        public void Lex_CarriageReturnInMiddleOfString_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"multi\rline\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Unterminated string.
1: " + "\"multi" + @"
         ^
2: line" + "\"" + @"
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_DashesInName_ThrowsExceptionWithCorrectMessage()
        {
            var token = new Lexer().Lex(new Source("a-b"));

            Assert.Equal(TokenKind.NAME, token.Kind);
            Assert.Equal(0, token.Start);
            Assert.Equal(1, token.End);
            Assert.Equal("a", token.Value);

            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("a-b"), token.End));

            Assert.Equal(("Syntax Error GraphQL (1:3) Invalid number, expected digit but got: \"b\"" + @"
1: a-b
     ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_IncompleteSpread_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("..")));

            Assert.Equal(("Syntax Error GraphQL (1:1) Unexpected character \".\"" + @"
1: ..
   ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidCharacter_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\u0007")));

            Assert.Equal((@"Syntax Error GraphQL (1:1) Invalid character " + "\"\\u0007\"" + @".
1: \u0007
   ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidEscapeSequenceXCharacter_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"bad \\x esc\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Invalid character escape sequence: \x.
1: " + "\"bad \\x esc\"" + @"
         ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidEscapeSequenceZetCharacter_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"bad \\z esc\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Invalid character escape sequence: \z.
1: " + "\"bad \\z esc\"" + @"
         ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidUnicode_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"bad \\u1 esc\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Invalid character escape sequence: \u1 es.
1: " + "\"bad \\u1 esc\"" + @"
         ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidUnicode2_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"bad \\u0XX1 esc\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Invalid character escape sequence: \u0XX1.
1: " + "\"bad \\u0XX1 esc\"" + @"
         ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidUnicode3_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"bad \\uFXXX esc\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Invalid character escape sequence: \uFXXX.
1: " + "\"bad \\uFXXX esc\"" + @"
         ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidUnicode4_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"bad \\uXXXX esc\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Invalid character escape sequence: \uXXXX.
1: " + "\"bad \\uXXXX esc\"" + @"
         ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_InvalidUnicode5_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"bad \\uXXXF esc\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Invalid character escape sequence: \uXXXF.
1: " + "\"bad \\uXXXF esc\"" + @"
         ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_LineBreakInMiddleOfString_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"multi\nline\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:7) Unterminated string.
1: " + "\"multi" + @"
         ^
2: line" + "\"" + @"
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_LonelyQuestionMark_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("?")));

            Assert.Equal(("Syntax Error GraphQL (1:1) Unexpected character \"?\"" + @"
1: ?
   ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_MissingExponentInNumber_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("1.0e")));

            Assert.Equal(("Syntax Error GraphQL (1:5) Invalid number, expected digit but got: <EOF>" + @"
1: 1.0e
       ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }


        [Fact]
        public void Lex_NonNumericCharacterInNumberExponent_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("1.0eA")));

            Assert.Equal(("Syntax Error GraphQL (1:5) Invalid number, expected digit but got: \"A\"" + @"
1: 1.0eA
       ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NonNumericCharInNumber_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("1.A")));

            Assert.Equal(("Syntax Error GraphQL (1:3) Invalid number, expected digit but got: \"A\"" + @"
1: 1.A
     ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NonNumericCharInNumber2_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("-A")));

            Assert.Equal(("Syntax Error GraphQL (1:2) Invalid number, expected digit but got: \"A\"" + @"
1: -A
    ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NotAllowedUnicode_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\\u203B")));

            Assert.Equal(("Syntax Error GraphQL (1:1) Unexpected character \"\\u203B\"" + @"
1: \u203B
   ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NotAllowedUnicode1_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\\u200b")));

            Assert.Equal(("Syntax Error GraphQL (1:1) Unexpected character \"\\u200b\"" + @"
1: \u200b
   ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NullByteInString_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"null-byte is not \u0000 end of file")));

            Assert.Equal((@"Syntax Error GraphQL (1:19) Invalid character within String: \u0000.
1: " + "\"null-byte is not \\u0000 end of file" + @"
                     ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NumberDoubleZeros_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("00")));

            Assert.Equal((@"Syntax Error GraphQL (1:2) Invalid number, unexpected digit after 0: " + "\"0\"" + @"
1: 00
    ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NumberNoDecimalPartEOFInstead_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("1.")));

            Assert.Equal(("Syntax Error GraphQL (1:3) Invalid number, expected digit but got: <EOF>" + @"
1: 1.
     ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NumberPlusOne_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("+1")));

            Assert.Equal(("Syntax Error GraphQL (1:1) Unexpected character \"+\"" + @"
1: +1
   ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_NumberStartingWithDot_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source(".123")));

            Assert.Equal(("Syntax Error GraphQL (1:1) Unexpected character \".\"" + @"
1: .123
   ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_UnescapedControlChar_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"contains unescaped \u0007 control char")));

            Assert.Equal((@"Syntax Error GraphQL (1:21) Invalid character within String: \u0007.
1: " + "\"contains unescaped \\u0007 control char" + @"
                       ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_UnterminatedString_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"")));

            Assert.Equal((@"Syntax Error GraphQL (1:2) Unterminated string.
1: " + "\"" + @"
    ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }

        [Fact]
        public void Lex_UnterminatedStringWithText_ThrowsExceptionWithCorrectMessage()
        {
            var exception = Assert.Throws<GraphQLSyntaxErrorException>(
                () => new Lexer().Lex(new Source("\"no end quote")));

            Assert.Equal((@"Syntax Error GraphQL (1:14) Unterminated string.
1: " + "\"no end quote" + @"
                ^
").Replace(Environment.NewLine, "\n"), exception.Message);
        }
    }
}

using Xunit;

namespace GraphQL.Language.Tests
{
    public class LexerTests
    {
        [Fact]
        public void Lex_ATPunctuation_HasCorrectEnd()
        {
            var token = GetATPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_ATPunctuation_HasCorrectKind()
        {
            var token = GetATPunctuationTokenLexer();
            Assert.Equal(TokenKind.AT, token.Kind);
        }

        [Fact]
        public void Lex_ATPunctuation_HasCorrectStart()
        {
            var token = GetATPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_ATPunctuation_HasCorrectValue()
        {
            var token = GetATPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_BangPunctuation_HasCorrectEnd()
        {
            var token = GetBangPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_BangPunctuation_HasCorrectKind()
        {
            var token = GetBangPunctuationTokenLexer();
            Assert.Equal(TokenKind.BANG, token.Kind);
        }

        [Fact]
        public void Lex_BangPunctuation_HasCorrectStart()
        {
            var token = GetBangPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_BangPunctuation_HasCorrectValue()
        {
            var token = GetBangPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_ColonPunctuation_HasCorrectEnd()
        {
            var token = GetColonPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_ColonPunctuation_HasCorrectKind()
        {
            var token = GetColonPunctuationTokenLexer();
            Assert.Equal(TokenKind.COLON, token.Kind);
        }

        [Fact]
        public void Lex_ColonPunctuation_HasCorrectStart()
        {
            var token = GetColonPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_ColonPunctuation_HasCorrectValue()
        {
            var token = GetColonPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_DollarPunctuation_HasCorrectEnd()
        {
            var token = GetDollarPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_DollarPunctuation_HasCorrectKind()
        {
            var token = GetDollarPunctuationTokenLexer();
            Assert.Equal(TokenKind.DOLLAR, token.Kind);
        }

        [Fact]
        public void Lex_DollarPunctuation_HasCorrectStart()
        {
            var token = GetDollarPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_DollarPunctuation_HasCorrectValue()
        {
            var token = GetDollarPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_EmptySource_ReturnsEOF()
        {
            var token = new Lexer().Lex(new Source(""));

            Assert.Equal(TokenKind.EOF, token.Kind);
        }

        [Fact]
        public void Lex_EqualsPunctuation_HasCorrectEnd()
        {
            var token = GetEqualsPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_EqualsPunctuation_HasCorrectKind()
        {
            var token = GetEqualsPunctuationTokenLexer();
            Assert.Equal(TokenKind.EQUALS, token.Kind);
        }

        [Fact]
        public void Lex_EqualsPunctuation_HasCorrectStart()
        {
            var token = GetEqualsPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_EqualsPunctuation_HasCorrectValue()
        {
            var token = GetEqualsPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_EscapedStringToken_HasCorrectEnd()
        {
            var token = GetEscapedStringTokenLexer();
            Assert.Equal(20, token.End);
        }

        [Fact]
        public void Lex_EscapedStringToken_HasCorrectStart()
        {
            var token = GetEscapedStringTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_EscapedStringToken_HasCorrectValue()
        {
            var token = GetEscapedStringTokenLexer();
            Assert.Equal("escaped \n\r\b\t\f", token.Value);
        }

        [Fact]
        public void Lex_EscapedStringToken_HasStringKind()
        {
            var token = GetEscapedStringTokenLexer();
            Assert.Equal(TokenKind.STRING, token.Kind);
        }

        [Fact]
        public void Lex_LeftBracePunctuation_HasCorrectEnd()
        {
            var token = GetLeftBracePunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_LeftBracePunctuation_HasCorrectKind()
        {
            var token = GetLeftBracePunctuationTokenLexer();
            Assert.Equal(TokenKind.BRACE_L, token.Kind);
        }

        [Fact]
        public void Lex_LeftBracePunctuation_HasCorrectStart()
        {
            var token = GetLeftBracePunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_LeftBracePunctuation_HasCorrectValue()
        {
            var token = GetLeftBracePunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_LeftBracketPunctuation_HasCorrectEnd()
        {
            var token = GetLeftBracketPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_LeftBracketPunctuation_HasCorrectKind()
        {
            var token = GetLeftBracketPunctuationTokenLexer();
            Assert.Equal(TokenKind.BRACKET_L, token.Kind);
        }

        [Fact]
        public void Lex_LeftBracketPunctuation_HasCorrectStart()
        {
            var token = GetLeftBracketPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_LeftBracketPunctuation_HasCorrectValue()
        {
            var token = GetLeftBracketPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_LeftParenthesisPunctuation_HasCorrectEnd()
        {
            var token = GetLeftParenthesisPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_LeftParenthesisPunctuation_HasCorrectKind()
        {
            var token = GetLeftParenthesisPunctuationTokenLexer();
            Assert.Equal(TokenKind.PAREN_L, token.Kind);
        }

        [Fact]
        public void Lex_LeftParenthesisPunctuation_HasCorrectStart()
        {
            var token = GetLeftParenthesisPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_LeftParenthesisPunctuation_HasCorrectValue()
        {
            var token = GetLeftParenthesisPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_MultipleDecimalsIntToken_HasCorrectEnd()
        {
            var token = GetMultipleDecimalsIntTokenLexer();
            Assert.Equal(3, token.End);
        }

        [Fact]
        public void Lex_MultipleDecimalsIntToken_HasCorrectStart()
        {
            var token = GetMultipleDecimalsIntTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_MultipleDecimalsIntToken_HasCorrectValue()
        {
            var token = GetMultipleDecimalsIntTokenLexer();
            Assert.Equal("123", token.Value);
        }

        [Fact]
        public void Lex_MultipleDecimalsIntToken_HasIntKind()
        {
            var token = GetMultipleDecimalsIntTokenLexer();
            Assert.Equal(TokenKind.INT, token.Kind);
        }

        [Fact]
        public void Lex_NameTokenWithComments_HasCorrectEnd()
        {
            var token = GetSingleNameTokenLexerWithComments();
            Assert.Equal(13, token.End);
        }

        [Fact]
        public void Lex_NameTokenWithComments_HasCorrectStart()
        {
            var token = GetSingleNameTokenLexerWithComments();
            Assert.Equal(10, token.Start);
        }

        [Fact]
        public void Lex_NameTokenWithComments_HasCorrectValue()
        {
            var token = GetSingleNameTokenLexerWithComments();
            Assert.Equal("foo", token.Value);
        }

        [Fact]
        public void Lex_NameTokenWithComments_HasNameKind()
        {
            var token = GetSingleNameTokenLexerWithComments();
            Assert.Equal(TokenKind.NAME, token.Kind);
        }

        [Fact]
        public void Lex_NameTokenWithWhitespaces_HasCorrectEnd()
        {
            var token = GetSingleNameTokenLexerSurroundedWithWhitespaces();
            Assert.Equal(12, token.End);
        }

        [Fact]
        public void Lex_NameTokenWithWhitespaces_HasCorrectStart()
        {
            var token = GetSingleNameTokenLexerSurroundedWithWhitespaces();
            Assert.Equal(9, token.Start);
        }

        [Fact]
        public void Lex_NameTokenWithWhitespaces_HasCorrectValue()
        {
            var token = GetSingleNameTokenLexerSurroundedWithWhitespaces();
            Assert.Equal("foo", token.Value);
        }

        [Fact]
        public void Lex_NameTokenWithWhitespaces_HasNameKind()
        {
            var token = GetSingleNameTokenLexerSurroundedWithWhitespaces();
            Assert.Equal(TokenKind.NAME, token.Kind);
        }

        [Fact]
        public void Lex_NullInput_ReturnsEOF()
        {
            var token = new Lexer().Lex(new Source(null));

            Assert.Equal(TokenKind.EOF, token.Kind);
        }

        [Fact]
        public void Lex_PipePunctuation_HasCorrectEnd()
        {
            var token = GetPipePunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_PipePunctuation_HasCorrectKind()
        {
            var token = GetPipePunctuationTokenLexer();
            Assert.Equal(TokenKind.PIPE, token.Kind);
        }

        [Fact]
        public void Lex_PipePunctuation_HasCorrectStart()
        {
            var token = GetPipePunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_PipePunctuation_HasCorrectValue()
        {
            var token = GetPipePunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_QuoteStringToken_HasCorrectEnd()
        {
            var token = GetQuoteStringTokenLexer();
            Assert.Equal(10, token.End);
        }

        [Fact]
        public void Lex_QuoteStringToken_HasCorrectStart()
        {
            var token = GetQuoteStringTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_QuoteStringToken_HasCorrectValue()
        {
            var token = GetQuoteStringTokenLexer();
            Assert.Equal("quote \"", token.Value);
        }

        [Fact]
        public void Lex_QuoteStringToken_HasStringKind()
        {
            var token = GetQuoteStringTokenLexer();
            Assert.Equal(TokenKind.STRING, token.Kind);
        }

        [Fact]
        public void Lex_RightBracePunctuation_HasCorrectEnd()
        {
            var token = GetRightBracePunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_RightBracePunctuation_HasCorrectKind()
        {
            var token = GetRightBracePunctuationTokenLexer();
            Assert.Equal(TokenKind.BRACE_R, token.Kind);
        }

        [Fact]
        public void Lex_RightBracePunctuation_HasCorrectStart()
        {
            var token = GetRightBracePunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_RightBracePunctuation_HasCorrectValue()
        {
            var token = GetRightBracePunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_RightBracketPunctuation_HasCorrectEnd()
        {
            var token = GetRightBracketPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_RightBracketPunctuation_HasCorrectKind()
        {
            var token = GetRightBracketPunctuationTokenLexer();
            Assert.Equal(TokenKind.BRACKET_R, token.Kind);
        }

        [Fact]
        public void Lex_RightBracketPunctuation_HasCorrectStart()
        {
            var token = GetRightBracketPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_RightBracketPunctuation_HasCorrectValue()
        {
            var token = GetRightBracketPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_RightParenthesisPunctuation_HasCorrectEnd()
        {
            var token = GetRightParenthesisPunctuationTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_RightParenthesisPunctuation_HasCorrectKind()
        {
            var token = GetRightParenthesisPunctuationTokenLexer();
            Assert.Equal(TokenKind.PAREN_R, token.Kind);
        }

        [Fact]
        public void Lex_RightParenthesisPunctuation_HasCorrectStart()
        {
            var token = GetRightParenthesisPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_RightParenthesisPunctuation_HasCorrectValue()
        {
            var token = GetRightParenthesisPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_SimpleStringToken_HasCorrectEnd()
        {
            var token = GetSimpleStringTokenLexer();
            Assert.Equal(5, token.End);
        }

        [Fact]
        public void Lex_SimpleStringToken_HasCorrectStart()
        {
            var token = GetSimpleStringTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SimpleStringToken_HasCorrectValue()
        {
            var token = GetSimpleStringTokenLexer();
            Assert.Equal("str", token.Value);
        }

        [Fact]
        public void Lex_SimpleStringToken_HasStringKind()
        {
            var token = GetSimpleStringTokenLexer();
            Assert.Equal(TokenKind.STRING, token.Kind);
        }

        [Fact]
        public void Lex_SingleDecimalIntToken_HasCorrectEnd()
        {
            var token = GetSingleDecimalIntTokenLexer();
            Assert.Equal(1, token.End);
        }

        [Fact]
        public void Lex_SingleDecimalIntToken_HasCorrectStart()
        {
            var token = GetSingleDecimalIntTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleDecimalIntToken_HasCorrectValue()
        {
            var token = GetSingleDecimalIntTokenLexer();
            Assert.Equal("0", token.Value);
        }

        [Fact]
        public void Lex_SingleDecimalIntToken_HasIntKind()
        {
            var token = GetSingleDecimalIntTokenLexer();
            Assert.Equal(TokenKind.INT, token.Kind);
        }

        [Fact]
        public void Lex_SingleFloatTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleFloatTokenLexer();
            Assert.Equal(5, token.End);
        }

        [Fact]
        public void Lex_SingleFloatTokenLexer_HasCorrectKind()
        {
            var token = GetSingleFloatTokenLexer();
            Assert.Equal(TokenKind.FLOAT, token.Kind);
        }

        [Fact]
        public void Lex_SingleFloatTokenLexer_HasCorrectStart()
        {
            var token = GetSingleFloatTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleFloatTokenLexer_HasCorrectValue()
        {
            var token = GetSingleFloatTokenLexer();
            Assert.Equal("4.123", token.Value);
        }

        [Fact]
        public void Lex_SingleFloatWithExplicitlyPositiveExponentTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleFloatWithExplicitlyPositiveExponentTokenLexer();
            Assert.Equal(6, token.End);
        }

        [Fact]
        public void Lex_SingleFloatWithExplicitlyPositiveExponentTokenLexer_HasCorrectKind()
        {
            var token = GetSingleFloatWithExplicitlyPositiveExponentTokenLexer();
            Assert.Equal(TokenKind.FLOAT, token.Kind);
        }

        [Fact]
        public void Lex_SingleFloatWithExplicitlyPositiveExponentTokenLexer_HasCorrectStart()
        {
            var token = GetSingleFloatWithExplicitlyPositiveExponentTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleFloatWithExplicitlyPositiveExponentTokenLexer_HasCorrectValue()
        {
            var token = GetSingleFloatWithExplicitlyPositiveExponentTokenLexer();
            Assert.Equal("123e+4", token.Value);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentCapitalLetterTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleFloatWithExponentCapitalLetterTokenLexer();
            Assert.Equal(5, token.End);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentCapitalLetterTokenLexer_HasCorrectKind()
        {
            var token = GetSingleFloatWithExponentCapitalLetterTokenLexer();
            Assert.Equal(TokenKind.FLOAT, token.Kind);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentCapitalLetterTokenLexer_HasCorrectStart()
        {
            var token = GetSingleFloatWithExponentCapitalLetterTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentCapitalLetterTokenLexer_HasCorrectValue()
        {
            var token = GetSingleFloatWithExponentCapitalLetterTokenLexer();
            Assert.Equal("123E4", token.Value);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleFloatWithExponentTokenLexer();
            Assert.Equal(5, token.End);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentTokenLexer_HasCorrectKind()
        {
            var token = GetSingleFloatWithExponentTokenLexer();
            Assert.Equal(TokenKind.FLOAT, token.Kind);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentTokenLexer_HasCorrectStart()
        {
            var token = GetSingleFloatWithExponentTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleFloatWithExponentTokenLexer_HasCorrectValue()
        {
            var token = GetSingleFloatWithExponentTokenLexer();
            Assert.Equal("123e4", token.Value);
        }

        [Fact]
        public void Lex_SingleFloatWithNegativeExponentTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleFloatWithNegativeExponentTokenLexer();
            Assert.Equal(6, token.End);
        }

        [Fact]
        public void Lex_SingleFloatWithNegativeExponentTokenLexer_HasCorrectKind()
        {
            var token = GetSingleFloatWithNegativeExponentTokenLexer();
            Assert.Equal(TokenKind.FLOAT, token.Kind);
        }

        [Fact]
        public void Lex_SingleFloatWithNegativeExponentTokenLexer_HasCorrectStart()
        {
            var token = GetSingleFloatWithNegativeExponentTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleFloatWithNegativeExponentTokenLexer_HasCorrectValue()
        {
            var token = GetSingleFloatWithNegativeExponentTokenLexer();
            Assert.Equal("123e-4", token.Value);
        }

        [Fact]
        public void Lex_SingleNameSurroundedByCommasTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleNameSurroundedByCommasTokenLexer();
            Assert.Equal(6, token.End);
        }

        [Fact]
        public void Lex_SingleNameSurroundedByCommasTokenLexer_HasCorrectKind()
        {
            var token = GetSingleNameSurroundedByCommasTokenLexer();
            Assert.Equal(TokenKind.NAME, token.Kind);
        }

        [Fact]
        public void Lex_SingleNameSurroundedByCommasTokenLexer_HasCorrectStart()
        {
            var token = GetSingleNameSurroundedByCommasTokenLexer();
            Assert.Equal(3, token.Start);
        }

        [Fact]
        public void Lex_SingleNameSurroundedByCommasTokenLexer_HasCorrectValue()
        {
            var token = GetSingleNameSurroundedByCommasTokenLexer();
            Assert.Equal("foo", token.Value);
        }

        [Fact]
        public void Lex_SingleNameWithBOMHeaderTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleNameWithBOMHeaderTokenLexer();
            Assert.Equal(5, token.End);
        }

        [Fact]
        public void Lex_SingleNameWithBOMHeaderTokenLexer_HasCorrectKind()
        {
            var token = GetSingleNameWithBOMHeaderTokenLexer();
            Assert.Equal(TokenKind.NAME, token.Kind);
        }

        [Fact]
        public void Lex_SingleNameWithBOMHeaderTokenLexer_HasCorrectStart()
        {
            var token = GetSingleNameWithBOMHeaderTokenLexer();
            Assert.Equal(2, token.Start);
        }

        [Fact]
        public void Lex_SingleNameWithBOMHeaderTokenLexer_HasCorrectValue()
        {
            var token = GetSingleNameWithBOMHeaderTokenLexer();
            Assert.Equal("foo", token.Value);
        }

        [Fact]
        public void Lex_SingleNegativeFloatTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleNegativeFloatTokenLexer();
            Assert.Equal(6, token.End);
        }

        [Fact]
        public void Lex_SingleNegativeFloatTokenLexer_HasCorrectKind()
        {
            var token = GetSingleNegativeFloatTokenLexer();
            Assert.Equal(TokenKind.FLOAT, token.Kind);
        }

        [Fact]
        public void Lex_SingleNegativeFloatTokenLexer_HasCorrectStart()
        {
            var token = GetSingleNegativeFloatTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleNegativeFloatTokenLexer_HasCorrectValue()
        {
            var token = GetSingleNegativeFloatTokenLexer();
            Assert.Equal("-0.123", token.Value);
        }

        [Fact]
        public void Lex_SingleNegativeFloatWithExponentTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleNegativeFloatWithExponentTokenLexer();
            Assert.Equal(6, token.End);
        }

        [Fact]
        public void Lex_SingleNegativeFloatWithExponentTokenLexer_HasCorrectKind()
        {
            var token = GetSingleNegativeFloatWithExponentTokenLexer();
            Assert.Equal(TokenKind.FLOAT, token.Kind);
        }

        [Fact]
        public void Lex_SingleNegativeFloatWithExponentTokenLexer_HasCorrectStart()
        {
            var token = GetSingleNegativeFloatWithExponentTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleNegativeFloatWithExponentTokenLexer_HasCorrectValue()
        {
            var token = GetSingleNegativeFloatWithExponentTokenLexer();
            Assert.Equal("-123e4", token.Value);
        }

        [Fact]
        public void Lex_SingleNegativeIntTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleNegativeIntTokenLexer();
            Assert.Equal(2, token.End);
        }

        [Fact]
        public void Lex_SingleNegativeIntTokenLexer_HasCorrectKind()
        {
            var token = GetSingleNegativeIntTokenLexer();
            Assert.Equal(TokenKind.INT, token.Kind);
        }

        [Fact]
        public void Lex_SingleNegativeIntTokenLexer_HasCorrectStart()
        {
            var token = GetSingleNegativeIntTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleNegativeIntTokenLexer_HasCorrectValue()
        {
            var token = GetSingleNegativeIntTokenLexer();
            Assert.Equal("-3", token.Value);
        }

        [Fact]
        public void Lex_SingleStringWithSlashesTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleStringWithSlashesTokenLexer();
            Assert.Equal(15, token.End);
        }

        [Fact]
        public void Lex_SingleStringWithSlashesTokenLexer_HasCorrectKind()
        {
            var token = GetSingleStringWithSlashesTokenLexer();
            Assert.Equal(TokenKind.STRING, token.Kind);
        }

        [Fact]
        public void Lex_SingleStringWithSlashesTokenLexer_HasCorrectStart()
        {
            var token = GetSingleStringWithSlashesTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleStringWithSlashesTokenLexer_HasCorrectValue()
        {
            var token = GetSingleStringWithSlashesTokenLexer();
            Assert.Equal("slashes \\ /", token.Value);
        }

        [Fact]
        public void Lex_SingleStringWithUnicodeCharactersTokenLexer_HasCorrectEnd()
        {
            var token = GetSingleStringWithUnicodeCharactersTokenLexer();
            Assert.Equal(34, token.End);
        }

        [Fact]
        public void Lex_SingleStringWithUnicodeCharactersTokenLexer_HasCorrectKind()
        {
            var token = GetSingleStringWithUnicodeCharactersTokenLexer();
            Assert.Equal(TokenKind.STRING, token.Kind);
        }

        [Fact]
        public void Lex_SingleStringWithUnicodeCharactersTokenLexer_HasCorrectStart()
        {
            var token = GetSingleStringWithUnicodeCharactersTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SingleStringWithUnicodeCharactersTokenLexer_HasCorrectValue()
        {
            var token = GetSingleStringWithUnicodeCharactersTokenLexer();
            Assert.Equal("unicode \u1234\u5678\u90AB\uCDEF", token.Value);
        }

        [Fact]
        public void Lex_SpreadPunctuation_HasCorrectEnd()
        {
            var token = GetSpreadPunctuationTokenLexer();
            Assert.Equal(3, token.End);
        }

        [Fact]
        public void Lex_SpreadPunctuation_HasCorrectKind()
        {
            var token = GetSpreadPunctuationTokenLexer();
            Assert.Equal(TokenKind.SPREAD, token.Kind);
        }

        [Fact]
        public void Lex_SpreadPunctuation_HasCorrectStart()
        {
            var token = GetSpreadPunctuationTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_SpreadPunctuation_HasCorrectValue()
        {
            var token = GetSpreadPunctuationTokenLexer();
            Assert.Null(token.Value);
        }

        [Fact]
        public void Lex_WhiteSpaceStringToken_HasCorrectEnd()
        {
            var token = GetWhiteSpaceStringTokenLexer();
            Assert.Equal(15, token.End);
        }

        [Fact]
        public void Lex_WhiteSpaceStringToken_HasCorrectStart()
        {
            var token = GetWhiteSpaceStringTokenLexer();
            Assert.Equal(0, token.Start);
        }

        [Fact]
        public void Lex_WhiteSpaceStringToken_HasCorrectValue()
        {
            var token = GetWhiteSpaceStringTokenLexer();
            Assert.Equal(" white space ", token.Value);
        }

        [Fact]
        public void Lex_WhiteSpaceStringToken_HasStringKind()
        {
            var token = GetWhiteSpaceStringTokenLexer();
            Assert.Equal(TokenKind.STRING, token.Kind);
        }

        private static Token GetATPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("@"));
        }

        private static Token GetBangPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("!"));
        }

        private static Token GetColonPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source(":"));
        }

        private static Token GetDollarPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("$"));
        }

        private static Token GetEqualsPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("="));
        }

        private static Token GetEscapedStringTokenLexer()
        {
            return new Lexer().Lex(new Source("\"escaped \\n\\r\\b\\t\\f\""));
        }

        private static Token GetLeftBracePunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("{"));
        }

        private static Token GetLeftBracketPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("["));
        }

        private static Token GetLeftParenthesisPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("("));
        }

        private static Token GetMultipleDecimalsIntTokenLexer()
        {
            return new Lexer().Lex(new Source("123"));
        }

        private static Token GetPipePunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("|"));
        }

        private static Token GetQuoteStringTokenLexer()
        {
            return new Lexer().Lex(new Source("\"quote \\\"\""));
        }

        private static Token GetRightBracePunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("}"));
        }

        private static Token GetRightBracketPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("]"));
        }

        private static Token GetRightParenthesisPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source(")"));
        }

        private static Token GetSimpleStringTokenLexer()
        {
            return new Lexer().Lex(new Source("\"str\""));
        }

        private static Token GetSingleDecimalIntTokenLexer()
        {
            return new Lexer().Lex(new Source("0"));
        }

        private static Token GetSingleFloatTokenLexer()
        {
            return new Lexer().Lex(new Source("4.123"));
        }

        private static Token GetSingleFloatWithExplicitlyPositiveExponentTokenLexer()
        {
            return new Lexer().Lex(new Source("123e+4"));
        }

        private static Token GetSingleFloatWithExponentCapitalLetterTokenLexer()
        {
            return new Lexer().Lex(new Source("123E4"));
        }

        private static Token GetSingleFloatWithExponentTokenLexer()
        {
            return new Lexer().Lex(new Source("123e4"));
        }

        private static Token GetSingleFloatWithNegativeExponentTokenLexer()
        {
            return new Lexer().Lex(new Source("123e-4"));
        }

        private static Token GetSingleNameSurroundedByCommasTokenLexer()
        {
            return new Lexer().Lex(new Source(",,,foo,,,"));
        }

        private static Token GetSingleNameTokenLexerSurroundedWithWhitespaces()
        {
            return new Lexer().Lex(new Source("\r\n        foo\r\n\r\n    "));
        }

        private static Token GetSingleNameTokenLexerWithComments()
        {
            return new Lexer().Lex(new Source("\r\n#comment\r\nfoo#comment"));
        }

        private static Token GetSingleNameWithBOMHeaderTokenLexer()
        {
            return new Lexer().Lex(new Source("\uFEFF foo\\"));
        }

        private static Token GetSingleNegativeFloatTokenLexer()
        {
            return new Lexer().Lex(new Source("-0.123"));
        }

        private static Token GetSingleNegativeFloatWithExponentTokenLexer()
        {
            return new Lexer().Lex(new Source("-123e4"));
        }

        private static Token GetSingleNegativeIntTokenLexer()
        {
            return new Lexer().Lex(new Source("-3"));
        }

        private static Token GetSingleStringWithSlashesTokenLexer()
        {
            return new Lexer().Lex(new Source("\"slashes \\\\ \\/\""));
        }

        private static Token GetSingleStringWithUnicodeCharactersTokenLexer()
        {
            return new Lexer().Lex(new Source("\"unicode \\u1234\\u5678\\u90AB\\uCDEF\""));
        }

        private static Token GetSpreadPunctuationTokenLexer()
        {
            return new Lexer().Lex(new Source("..."));
        }

        private static Token GetWhiteSpaceStringTokenLexer()
        {
            return new Lexer().Lex(new Source("\" white space \""));
        }
    }
}

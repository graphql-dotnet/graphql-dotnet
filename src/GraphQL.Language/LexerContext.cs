using System;
using System.Globalization;

namespace GraphQL.Language
{
    public class LexerContext : IDisposable
    {
        private int currentIndex;
        private ISource source;

        public LexerContext(ISource source, int index)
        {
            this.currentIndex = index;
            this.source = source;
        }

        public void Dispose()
        {
        }

        public Token GetToken()
        {
            if (this.source.Body == null)
                return this.CreateEOFToken();

            this.currentIndex = this.GetPositionAfterWhitespace(this.source.Body, this.currentIndex);

            if (this.currentIndex >= this.source.Body.Length)
                return this.CreateEOFToken();

            var unicode = this.IfUnicodeGetString();

            var code = this.source.Body[this.currentIndex];

            this.ValidateCharacterCode(code);

            var token = this.CheckForPunctuationTokens(code);
            if (token != null)
                return token;

            if (char.IsLetter(code) || code == '_')
                return this.ReadName();

            if (char.IsNumber(code) || code == '-')
                return this.ReadNumber();

            if (code == '"')
                return this.ReadString();

            throw new GraphQLSyntaxErrorException(
                $"Unexpected character {this.ResolveCharName(code, unicode)}", this.source, this.currentIndex);
        }

        public bool OnlyHexInString(string test)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        public Token ReadNumber()
        {
            var isFloat = false;
            var start = this.currentIndex;
            var code = this.source.Body[start];

            if (code == '-')
                code = this.NextCode();

            var nextCode = code == '0'
                ? this.NextCode()
                : this.ReadDigitsFromOwnSource(code);

            if (nextCode >= 48 && nextCode <= 57)
            {
                throw new GraphQLSyntaxErrorException(
                    $"Invalid number, unexpected digit after {code}: \"{nextCode}\"", this.source, this.currentIndex);
            }

            code = nextCode;
            if (code == '.')
            {
                isFloat = true;
                code = this.ReadDigitsFromOwnSource(this.NextCode());
            }

            if (code == 'E' || code == 'e')
            {
                isFloat = true;
                code = this.NextCode();
                if (code == '+' || code == '-')
                {
                    code = this.NextCode();
                }

                code = this.ReadDigitsFromOwnSource(code);
            }

            return isFloat ? this.CreateFloatToken(start) : this.CreateIntToken(start);
        }

        public Token ReadString()
        {
            var start = this.currentIndex;
            var value = this.ProcessStringChunks();

            return new Token()
            {
                Kind = TokenKind.STRING,
                Value = value,
                Start = start,
                End = this.currentIndex + 1
            };
        }

        private static bool IsValidNameCharacter(char code)
        {
            return code == '_' || char.IsLetterOrDigit(code);
        }

        private string AppendCharactersFromLastChunk(string value, int chunkStart)
        {
            return value + this.source.Body.Substring(chunkStart, this.currentIndex - chunkStart - 1);
        }

        private string AppendToValueByCode(string value, char code)
        {
            switch (code)
            {
                case '"': value += '"'; break;
                case '/': value += '/'; break;
                case '\\': value += '\\'; break;
                case 'b': value += '\b'; break;
                case 'f': value += '\f'; break;
                case 'n': value += '\n'; break;
                case 'r': value += '\r'; break;
                case 't': value += '\t'; break;
                case 'u': value += this.GetUnicodeChar(); break;
                default:
                    throw new GraphQLSyntaxErrorException($"Invalid character escape sequence: \\{code}.", this.source, this.currentIndex);
            }

            return value;
        }

        private Token CreateEOFToken()
        {
            return new Token()
            {
                Start = this.currentIndex,
                End = this.currentIndex,
                Kind = TokenKind.EOF
            };
        }

        private Token CreateFloatToken(int start)
        {
            return new Token()
            {
                Kind = TokenKind.FLOAT,
                Start = start,
                End = this.currentIndex,
                Value = Convert.ToDouble(this.source.Body.Substring(start, this.currentIndex - start), CultureInfo.InvariantCulture)
            };
        }

        private Token CreateIntToken(int start)
        {
            var str = this.source.Body.Substring(start, this.currentIndex - start);

            object value = null;
            int intResult;
            long longResult;

            if (int.TryParse(str, out intResult))
            {
                value = intResult;
            }
            // If the value doesn't fit in an integer, revert to using long...
            else if (long.TryParse(str, out longResult))
            {
                value = longResult;
            }

            return new Token()
            {
                Kind = TokenKind.INT,
                Start = start,
                End = this.currentIndex,
                Value = value
            };
        }

        private Token CreateNameToken(int start)
        {
            return new Token()
            {
                Start = start,
                End = this.currentIndex,
                Kind = TokenKind.NAME,
                Value = this.source.Body.Substring(start, this.currentIndex - start)
            };
        }

        private Token CreatePunctuationToken(TokenKind kind, int offset)
        {
            return new Token()
            {
                Start = this.currentIndex,
                End = this.currentIndex + offset,
                Kind = kind,
                Value = null
            };
        }

        private char GetCode()
        {
            return this.IsNotAtTheEndOfQuery()
                ? this.source.Body[this.currentIndex]
                : (char)0;
        }

        private int GetPositionAfterWhitespace(string body, int start)
        {
            var position = start;

            while (position < body.Length)
            {
                var code = body[position];
                switch (code)
                {
                    case '\xFEFF': // BOM
                    case '\t': // tab
                    case ' ': // space
                    case '\n': // new line
                    case '\r': // carriage return
                    case ',': // Comma
                        ++position;
                        break;

                    case '#':
                        position = this.WaitForEndOfComment(body, position, code);
                        break;

                    default:
                        return position;
                }
            }

            return position;
        }

        private char GetUnicodeChar()
        {
            var expression = this.source.Body.Substring(this.currentIndex, 5);

            if (!this.OnlyHexInString(expression.Substring(1)))
            {
                throw new GraphQLSyntaxErrorException($"Invalid character escape sequence: \\{expression}.", this.source, this.currentIndex);
            }

            var character = (char)(
                this.CharToHex(this.NextCode()) << 12 |
                this.CharToHex(this.NextCode()) << 8 |
                this.CharToHex(this.NextCode()) << 4 |
                this.CharToHex(this.NextCode()));

            return character;
        }

        private int CharToHex(char code)
        {
            return Convert.ToByte(code.ToString(), 16);
        }

        private void CheckForInvalidCharacters(char code)
        {
            if (code < 0x0020 && code != 0x0009)
            {
                throw new GraphQLSyntaxErrorException(
                    $"Invalid character within String: \\u{((int)code).ToString("D4")}.", this.source, this.currentIndex);
            }
        }

        private Token CheckForPunctuationTokens(char code)
        {
            switch (code)
            {
                case '!': return this.CreatePunctuationToken(TokenKind.BANG, 1);
                case '$': return this.CreatePunctuationToken(TokenKind.DOLLAR, 1);
                case '(': return this.CreatePunctuationToken(TokenKind.PAREN_L, 1);
                case ')': return this.CreatePunctuationToken(TokenKind.PAREN_R, 1);
                case '.': return this.CheckForSpreadOperator();
                case ':': return this.CreatePunctuationToken(TokenKind.COLON, 1);
                case '=': return this.CreatePunctuationToken(TokenKind.EQUALS, 1);
                case '@': return this.CreatePunctuationToken(TokenKind.AT, 1);
                case '[': return this.CreatePunctuationToken(TokenKind.BRACKET_L, 1);
                case ']': return this.CreatePunctuationToken(TokenKind.BRACKET_R, 1);
                case '{': return this.CreatePunctuationToken(TokenKind.BRACE_L, 1);
                case '|': return this.CreatePunctuationToken(TokenKind.PIPE, 1);
                case '}': return this.CreatePunctuationToken(TokenKind.BRACE_R, 1);
                default: return null;
            }
        }

        private Token CheckForSpreadOperator()
        {
            var char1 = this.source.Body.Length > this.currentIndex + 1 ? this.source.Body[this.currentIndex + 1] : 0;
            var char2 = this.source.Body.Length > this.currentIndex + 2 ? this.source.Body[this.currentIndex + 2] : 0;

            if (char1 == '.' && char2 == '.')
            {
                return this.CreatePunctuationToken(TokenKind.SPREAD, 3);
            }

            return null;
        }

        private void CheckStringTermination(char code)
        {
            if (code != '"')
            {
                throw new GraphQLSyntaxErrorException("Unterminated string.", this.source, this.currentIndex);
            }
        }

        private string IfUnicodeGetString()
        {
            return this.source.Body.Length > this.currentIndex + 5 &&
                this.OnlyHexInString(this.source.Body.Substring(this.currentIndex + 2, 4))
                ? this.source.Body.Substring(this.currentIndex, 6)
                : null;
        }

        private char NextCode()
        {
            this.currentIndex++;
            return this.IsNotAtTheEndOfQuery()
                ? this.source.Body[this.currentIndex]
                : (char)0;
        }

        private char ProcessCharacter(ref string value, ref int chunkStart)
        {
            var code = this.GetCode();
            ++this.currentIndex;

            if (code == '\\')
            {
                value = this.AppendToValueByCode(this.AppendCharactersFromLastChunk(value, chunkStart), this.GetCode());

                ++this.currentIndex;
                chunkStart = this.currentIndex;
            }

            return this.GetCode();
        }

        private string ProcessStringChunks()
        {
            var chunkStart = ++this.currentIndex;
            var code = this.GetCode();
            var value = string.Empty;

            while (this.IsNotAtTheEndOfQuery() && code != 0x000A && code != 0x000D && code != '"')
            {
                this.CheckForInvalidCharacters(code);
                code = this.ProcessCharacter(ref value, ref chunkStart);
            }

            this.CheckStringTermination(code);
            value += this.source.Body.Substring(chunkStart, this.currentIndex - chunkStart);
            return value;
        }

        private int ReadDigits(ISource source, int start, char firstCode)
        {
            var body = source.Body;
            var position = start;
            var code = firstCode;

            if (!char.IsNumber(code))
            {
                throw new GraphQLSyntaxErrorException(
                    $"Invalid number, expected digit but got: {this.ResolveCharName(code)}", this.source, this.currentIndex);
            }

            do
            {
                code = ++position < body.Length
                    ? body[position]
                    : (char)0;
            }
            while (char.IsNumber(code));

            return position;
        }

        private char ReadDigitsFromOwnSource(char code)
        {
            this.currentIndex = this.ReadDigits(this.source, this.currentIndex, code);
            code = this.GetCode();
            return code;
        }

        private Token ReadName()
        {
            var start = this.currentIndex;
            var code = (char)0;

            do
            {
                this.currentIndex++;
                code = this.GetCode();
            }
            while (this.IsNotAtTheEndOfQuery() && IsValidNameCharacter(code));

            return this.CreateNameToken(start);
        }

        private bool IsNotAtTheEndOfQuery()
        {
            return this.currentIndex < this.source.Body.Length;
        }

        private string ResolveCharName(char code, string unicodeString = null)
        {
            if (code == '\0')
                return "<EOF>";

            if (!string.IsNullOrWhiteSpace(unicodeString))
                return $"\"{unicodeString}\"";

            return $"\"{code}\"";
        }

        private void ValidateCharacterCode(int code)
        {
            if (code < 0x0020 && code != 0x0009 && code != 0x000A && code != 0x000D)
            {
                throw new GraphQLSyntaxErrorException(
                    $"Invalid character \"\\u{code.ToString("D4")}\".", this.source, this.currentIndex);
            }
        }

        private int WaitForEndOfComment(string body, int position, char code)
        {
            while (++position < body.Length && (code = body[position]) != 0 && (code > 0x001F || code == 0x0009) && code != 0x000A && code != 0x000D)
            {
            }

            return position;
        }
    }
}

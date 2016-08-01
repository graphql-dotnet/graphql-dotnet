/*
https://github.com/sprache/Sprache

The MIT License

Copyright(c) 2011 Nicholas Blumhardt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GraphQL.Language
{
    /// <summary>
    /// Parsers and combinators.
    /// </summary>
    public static partial class Parse
    {
        /// <summary>
        /// Parse any character.
        /// </summary>
        public static readonly Parser<char> AnyChar = Char(c => true, "any character");

        /// <summary>
        /// Parse a whitespace.
        /// </summary>
        public static readonly Parser<char> WhiteSpace = Char(char.IsWhiteSpace, "whitespace");

        /// <summary>
        /// Parse a digit.
        /// </summary>
        public static readonly Parser<char> Digit = Char(char.IsDigit, "digit");

        /// <summary>
        /// Parse a letter.
        /// </summary>
        public static readonly Parser<char> Letter = Char(char.IsLetter, "letter");

        /// <summary>
        /// Parse a letter or digit.
        /// </summary>
        public static readonly Parser<char> LetterOrDigit = Char(char.IsLetterOrDigit, "letter or digit");

        /// <summary>
        /// Parse a lowercase letter.
        /// </summary>
        public static readonly Parser<char> Lower = Char(char.IsLower, "lowercase letter");

        /// <summary>
        /// Parse an uppercase letter.
        /// </summary>
        public static readonly Parser<char> Upper = Char(char.IsUpper, "uppercase letter");

        /// <summary>
        /// Parse a numeric character.
        /// </summary>
        public static readonly Parser<char> Numeric = Char(char.IsNumber, "numeric character");

        public static readonly Parser<char> LeftParen = Char(c => c == '(', "left paren");
        public static readonly Parser<char> RightParen = Char(c => c == ')', "right paren");
        public static readonly Parser<char> LeftBrace = Char(c => c == '{', "left brace");
        public static readonly Parser<char> RightBrace = Char(c => c == '}', "right brace");
        public static readonly Parser<char> LeftBracket = Char(c => c == '[', "left bracket");
        public static readonly Parser<char> RightBracket = Char(c => c == ']', "right bracket");
        public static readonly Parser<char> Dollar = Char(c => c == '$', "dollar");
        public static readonly Parser<char> Bang = Char(c => c == '!', "bang");
        public static readonly Parser<char> Colon = Char(c => c == ':', "colon");
        public static readonly Parser<char> At = Char(c => c == '@', "at");
        public static readonly Parser<char> Dot = Char(c => c == '.', "dot");
        public static readonly Parser<char> Eq = Char(c => c == '=', "equals");
        public static readonly Parser<char> Minus = Char(c => c == '-', "minus");
        public static readonly Parser<char> Sign = Char(c => c == '-' || c == '+', "minus");
        public static readonly Parser<char> NonZeroDigit = CharRegex("[1-9]", "non zero digit");
        public static readonly Parser<char> Zero = Char(c => c == '0', "zero");
        public static readonly Parser<char> Comma = Char(c => c == ',', "comma");

        /// <summary>
        /// TryParse a single character matching 'predicate'
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static Parser<char> Char(Predicate<char> predicate, string description)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return i =>
            {
                if (!i.AtEnd)
                {
                    if (predicate(i.Current))
                        return Result.Success(i.Current, Position.FromInput(i), i.Advance());

                    return Result.Failure<char>(i,
                        $"unexpected '{i.Current}'",
                        new[] {description});
                }

                return Result.Failure<char>(i,
                    "Unexpected end of input reached",
                    new[] {description});
            };
        }

        public static Parser<char> CharRegex(string pattern, string description)
        {
            return Char(c => System.Text.RegularExpressions.Regex.IsMatch(c.ToString(), pattern), description);
        }

        /// <summary>
        /// Parse first, if it succeeds, return first, otherwise try second.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Parser<T> Or<T>(this Parser<T> first, Parser<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i =>
            {
                var fr = first(i);
                if (!fr.WasSuccessful)
                {
                    return second(i).IfFailure(sf => DetermineBestError(fr, sf));
                }

                if (fr.Remainder.Equals(i))
                    return second(i).IfFailure(sf => fr);

                return fr;
            };
        }

        /// <summary>
        /// Parse first, if it succeeds, return first, otherwise try second.
        /// Assumes that the first parsed character will determine the parser chosen (see Try).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Parser<T> XOr<T>(this Parser<T> first, Parser<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => {
                var fr = first(i);
                if (!fr.WasSuccessful)
                {
                    // The 'X' part
                    if (!fr.Remainder.Equals(i))
                        return fr;

                    return second(i).IfFailure(sf => DetermineBestError(fr, sf));
                }

                // This handles a zero-length successful application of first.
                if (fr.Remainder.Equals(i))
                    return second(i).IfFailure(sf => fr);

                return fr;
            };
        }

        // Examines two results presumably obtained at an "Or" junction; returns the result with
        // the most information, or if they apply at the same input position, a union of the results.
        static IResult<T> DetermineBestError<T>(IResult<T> firstFailure, IResult<T> secondFailure)
        {
            if (secondFailure.Remainder.Position > firstFailure.Remainder.Position)
                return secondFailure;

            if (secondFailure.Remainder.Position == firstFailure.Remainder.Position)
                return Result.Failure<T>(
                    firstFailure.Remainder,
                    firstFailure.Message,
                    firstFailure.Expectations.Union(secondFailure.Expectations));

            return firstFailure;
        }

        public static Parser<IEnumerable<T>> Once<T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return parser.Select(r => (IEnumerable<T>)new[] { r });
        }

        public static Parser<IEnumerable<T>> Many<T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return i =>
            {
                var remainder = i;
                var result = new List<T>();
                var r = parser(i);

                while (r.WasSuccessful)
                {
                    if (remainder.Equals(r.Remainder))
                        break;

                    result.Add(r.Value);
                    remainder = r.Remainder;
                    r = parser(remainder);
                }

                return Result.Success<IEnumerable<T>>(result, Position.FromInput(i), remainder);
            };
        }

        public static Parser<T> Return<T>(T value)
        {
            return i => Result.Success(value, Position.FromInput(i),  i);
        }

        public static Parser<U> Return<T, U>(this Parser<T> parser, U value)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            return parser.Select(t => value);
        }

        public static Parser<U> Select<T, U>(this Parser<T> parser, Func<T, U> convert)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (convert == null) throw new ArgumentNullException(nameof(convert));

            return parser.Then(t => Return(convert(t)));
        }

        public static Parser<V> SelectMany<T, U, V>(
            this Parser<T> parser,
            Func<T, Parser<U>> selector,
            Func<T, U, V> projector)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (projector == null) throw new ArgumentNullException(nameof(projector));

            return parser.Then(t => selector(t).Select(u => projector(t, u)));
        }

        /// <summary>
        /// Chain a left-associative operator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOp"></typeparam>
        /// <param name="op"></param>
        /// <param name="operand"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public static Parser<T> ChainOperator<T, TOp>(
            Parser<TOp> op,
            Parser<T> operand,
            Func<TOp, T, T, T> apply)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return operand.Then(first => ChainOperatorRest(first, op, operand, apply, Or));
        }

        /// <summary>
        /// Chain a left-associative operator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOp"></typeparam>
        /// <param name="op"></param>
        /// <param name="operand"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public static Parser<T> XChainOperator<T, TOp>(
            Parser<TOp> op,
            Parser<T> operand,
            Func<TOp, T, T, T> apply)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return operand.Then(first => ChainOperatorRest(first, op, operand, apply, XOr));
        }

        static Parser<T> ChainOperatorRest<T, TOp>(
            T firstOperand,
            Parser<TOp> op,
            Parser<T> operand,
            Func<TOp, T, T, T> apply,
            Func<Parser<T>, Parser<T>, Parser<T>> or)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return or(op.Then(opvalue =>
                          operand.Then(operandValue =>
                              ChainOperatorRest(apply(opvalue, firstOperand, operandValue), op, operand, apply, or))),
                      Return(firstOperand));
        }

        public static Parser<U> Then<T, U>(this Parser<T> first, Func<T, Parser<U>> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(s => second(s.Value)(s.Remainder));
        }

        public static Parser<U> Return<T, U>(this Parser<T> first, Func<IResult<T>, U> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(s =>
            {
                var result = second(s);
                return Result.Success(result, s.Position, s.Remainder);
            });
        }

        public static Parser<U> Then<T, U>(
            this Parser<T> first,
            Parser<U> second,
            Func<IResult<T>, U> success)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(s =>
            {
                return second(s.Remainder).IfSuccess(rest =>
                {
                    var result = success(s);
                    return Result.Success(result, s.Position, rest.Remainder);
                });
            });
        }

        public static Parser<U> Then<T, Y, U>(
            this Parser<T> first,
            Parser<Y> second,
            Func<IResult<T>, IResult<Y>, U> success)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(s =>
            {
                return second(s.Remainder).IfSuccess(rest =>
                {
                    var result = success(s, rest);
                    return Result.Success(result, s.Position ?? rest.Position, rest.Remainder);
                });
            });
        }

        public static Parser<U> Then<T, Y, X, U>(
            this Parser<T> first,
            Parser<Y> second,
            Parser<X> third,
            Func<IResult<T>, IResult<Y>, IResult<X>, U> success)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(s =>
            {
                return second(s.Remainder).IfSuccess(middle =>
                {
                    return third(middle.Remainder).IfSuccess(rest =>
                    {
                        var result = success(s, middle, rest);
                        return Result.Success(result, s.Position ?? middle.Position ?? rest.Position, rest.Remainder);
                    });
                });
            });
        }

        public static Parser<U> Then<T, Y, X, V, U>(
            this Parser<T> first,
            Parser<Y> second,
            Parser<X> third,
            Parser<V> fourth,
            Func<IResult<T>, IResult<Y>, IResult<X>, IResult<V>,  U> success)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(fs =>
            {
                return second(fs.Remainder).IfSuccess(ss =>
                {
                    return third(ss.Remainder).IfSuccess(ts =>
                    {
                        return fourth(ts.Remainder).IfSuccess(last =>
                        {
                            var result = success(fs, ss, ts, last);
                            return Result.Success(result, fs.Position ?? ss.Position ?? ts.Position ?? last.Position, last.Remainder);
                        });
                    });
                });
            });
        }

        public static Parser<U> Then<T, Y, X, V, W, U>(
            this Parser<T> first,
            Parser<Y> second,
            Parser<X> third,
            Parser<V> fourth,
            Parser<W> fifth,
            Func<IResult<T>, IResult<Y>, IResult<X>, IResult<V>, IResult<W>,  U> success)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(fs =>
            {
                return second(fs.Remainder).IfSuccess(ss =>
                {
                    return third(ss.Remainder).IfSuccess(ts =>
                    {
                        return fourth(ts.Remainder).IfSuccess(four =>
                        {
                            return fifth(four.Remainder).IfSuccess(last =>
                            {
                                var result = success(fs, ss, ts, four, last);
                                return Result.Success(
                                    result,
                                    fs.Position ?? ss.Position ?? ts.Position ?? four.Position ?? last.Position,
                                    last.Remainder);
                            });
                        });
                    });
                });
            });
        }

        public static Parser<U> Then<T, Y, U>(
            this Parser<T> first,
            Parser<Y> second,
            Func<IResult<T>, IResult<Y>, IResult<U>> success)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(s =>
            {
                return second(s.Remainder).IfSuccess(rest => success(s, rest));
            });
        }

        public static Parser<T> Except<T, U>(this Parser<T> parser, Parser<U> except)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (except == null) throw new ArgumentNullException(nameof(except));

            return i =>
            {
                var r = except(i);
                if (r.WasSuccessful)
                    return Result.Failure<T>(i, "Excepted parser succeeded.", new[] { "other than the excepted input" });
                return parser(i);
            };
        }

        public static Parser<IEnumerable<T>> Until<T, U>(this Parser<T> parser, Parser<U> until)
        {
            return parser.Except(until).Many().Then(r => until.Return(r));
        }

        public static Parser<IEnumerable<char>> WhitespaceOrComma =>
            WhiteSpace.Many().Or(Comma.Many());

        public static Parser<T> Token<T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return i =>
            {
                var ws = WhitespaceOrComma.Many()(i);
                var result = parser(ws.Remainder);
                return result.IfSuccess(s =>
                {
                    var ws2 = WhitespaceOrComma.Many()(s.Remainder);
                    return Result.Success(result.Value, result.Position, ws2.Remainder);
                });
            };
        }

        /// <summary>
        /// Refer to another parser indirectly. This allows circular compile-time dependency between parsers.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Parser<T> Ref<T>(Func<Parser<T>> reference)
        {
            if (reference == null) throw new ArgumentNullException("reference");

            Parser<T> p = null;

            return i =>
            {
                if (p == null)
                    p = reference();

                if (i.Memos.ContainsKey(p))
                    throw new Exception(i.Memos[p].ToString());

                i.Memos[p] = Result.Failure<T>(i,
                    "Left recursion in the grammar.",
                    new string[0]);
                var result = p(i);
                i.Memos[p] = result;
                return result;
            };
        }
    }
}

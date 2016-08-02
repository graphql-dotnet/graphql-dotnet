using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GraphQL.Language
{
    partial class Parse
    {
        public static Parser<string> StringLiteral
            => Char('"').Then(CharExcept('"').Many(), Char('"'), (start, middle, end)
                => $"\"{middle.Value.ToStr()}\"");

        public static Parser<object> IntOrLong =>
            IntegerPart.Return<string, object>(f =>
            {
                int number;
                if (int.TryParse(f.Value, out number))
                {
                    return number;
                }

                long longNumber;
                if (long.TryParse(f.Value, out longNumber))
                {
                    return longNumber;
                }

                throw new ArgumentOutOfRangeException($"Value {f.Value} is not a valid int or long.");
            });

        public static Parser<string> IntegerPart =>
            ZeroWithSign.Or(
                Minus.Optional().Then(
                    NonZeroDigit.Once(),
                    Digit.Many().Optional(),
                    (first, middle, last) =>
                    {
                        var sign = first.Value.IsEmpty ? "" : first.Value.Get().ToString();
                        var rest = last.Value.Get();
                        var number = middle.Value.Concat(rest).ToStr();
                        return $"{sign}{number}";
                    }));

        public static Parser<double> Double =>
            IntegerPart.Then(FractionalPart, ExponentPart.Optional(), (integer, fraction, exponent) =>
            {
                var expVal = exponent.Value.GetOrElse("");

                double value;
                double.TryParse($"{integer.Value}{fraction.Value}{expVal}", out value);
                return value;
            }).Or(IntegerPart.Then(ExponentPart, (integer, exponent) =>
            {
                double value;
                double.TryParse($"{integer.Value}{exponent.Value}", out value);
                return value;
            }));

        public static Parser<string> FractionalPart =>
            Dot.Then(Digit.Many(), (first, rest) => $"{first.Value}{rest.Value.ToStr()}");

        public static Parser<string> ExponentPart =>
            CharRegex("[eE]", "exponent indicator").Then(
                Sign.Optional(),
                Digit.Many(),
                (first, middle, last) =>
                {
                    var sign = middle.Value.IsEmpty ? "" : middle.Value.Get().ToString();
                    var digits = last.Value.ToStr();
                    return $"{first.Value}{sign}{digits}";
                });

        public static Parser<string> ZeroWithSign =>
            Minus.Optional().Then(Zero, (first, rest) =>
            {
                var sign = first.Value.IsEmpty ? "" : first.Value.Get().ToString();
                var number = rest.Value.ToString();
                return $"{sign}{number}";
            });

        public static Parser<bool> Bool =>
            Parse.Regex(new Regex("(true|false)"), "boolean").Return(f =>
            {
                bool value;
                bool.TryParse(f.Value, out value);
                return value;
            });

        public static Parser<U> Parens<T, U>(this Parser<T> parser, Func<Position, IResult<T>, U> result) =>
            LeftParen.Once().Token().Then(parser, RightParen.Once().Token(), (l, p, r) => result(l.Position, p));

        public static Parser<U> Brackets<T, U>(this Parser<T> parser, Func<Position, IResult<T>, U> result) =>
            LeftBracket.Token().Then(parser, RightBracket.Token(), (l, p, r) => result(l.Position, p));

        public static Parser<U> Braces<T, U>(this Parser<T> parser, Func<Position, IResult<T>, U> result) =>
            LeftBrace.Token().Then(parser, RightBrace.Token(), (l, p, r) => result(l.Position, p));

        public static Parser<U> EmptyBraces<U>(Func<Position, IInput, U> result) =>
            LeftBrace.Token().Then(RightBrace.Token(), (l, r) => result(l.Position, r.Remainder));

        public static Parser<U> EmptyBrackets<U>(Func<Position, U> result) =>
            LeftBracket.Token().Then(RightBracket.Token(), (l, r) => result(l.Position));
    }
}

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
using System.Text.RegularExpressions;

namespace GraphQL.Language
{
    partial class Parse
    {
        /// <summary>
        /// Construct a parser from the given regular expression.
        /// </summary>
        /// <param name="pattern">The regex expression.</param>
        /// <param name="description">Description of characters that don't match.</param>
        /// <returns>a parse of string</returns>
        public static Parser<string> Regex(string pattern, string description = null)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));

            return Regex(new Regex(pattern), description);
        }

        /// <summary>
        /// Construct a parser from the given regular expression.
        /// </summary>
        /// <param name="regex">The regex expression.</param>
        /// <param name="description">Description of characters that don't match.</param>
        /// <returns>a parse of string</returns>
        public static Parser<string> Regex(Regex regex, string description = null)
        {
            if (regex == null) throw new ArgumentNullException(nameof(regex));

            return RegexMatch(regex, description).Return(match => match.Value.Value);
        }

        /// <summary>
        /// Construct a parser from the given regular expression, returning a parser of
        /// type <see cref="Match"/>.
        /// </summary>
        /// <param name="pattern">The regex expression.</param>
        /// <param name="description">Description of characters that don't match.</param>
        /// <returns>A parser of regex match objects.</returns>
        public static Parser<Match> RegexMatch(string pattern, string description = null)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));

            return RegexMatch(new Regex(pattern), description);
        }

        /// <summary>
        /// Construct a parser from the given regular expression, returning a parser of
        /// type <see cref="Match"/>.
        /// </summary>
        /// <param name="regex">The regex expression.</param>
        /// <param name="description">Description of characters that don't match.</param>
        /// <returns>A parser of regex match objects.</returns>
        public static Parser<Match> RegexMatch(Regex regex, string description = null)
        {
            if (regex == null) throw new ArgumentNullException(nameof(regex));

            regex = OptimizeRegex(regex);

            var expectations = description == null
                ? new string[0]
                : new[] { description };

            return i =>
            {
                if (!i.AtEnd)
                {
                    var remainder = i;
                    var input = i.Source.Substring(i.Position);
                    var match = regex.Match(input);

                    if (match.Success)
                    {
                        for (var j = 0; j < match.Length; j++)
                        {
                            remainder = remainder.Advance();
                        }

                        return Result.Success(match, Position.FromInput(i), remainder);
                    }

                    var found = match.Index == input.Length
                                    ? "end of source"
                                    : $"'{input[match.Index]}'";
                    return Result.Failure<Match>(
                        remainder,
                        $"string matching regex \'{regex.ToString()}\' expected but {found} found",
                        expectations);
                }

                return Result.Failure<Match>(i, "Unexpected end of input", expectations);
            };
        }

        private static readonly Dictionary<string, Regex> optimizedRegexCache = new Dictionary<string, Regex>();
        /// <summary>
        /// Optimize the regex by only matching successfully at the start of the input.
        /// Do this by wrapping the whole regex in non-capturing parentheses preceded by
        ///  a `^'.
        /// </summary>
        /// <remarks>
        /// This method is invoked via reflection in unit tests. If renamed, the tests
        /// will need to be modified or they will fail.
        /// </remarks>
        private static Regex OptimizeRegex(Regex regex) {
            var key = $"^(?:{regex})";
            if(optimizedRegexCache.ContainsKey(key)) {
                return optimizedRegexCache[key];
            }
            var newRegex = new Regex(key, regex.Options | RegexOptions.Compiled);
            optimizedRegexCache.Add(key, newRegex);
            return newRegex;
        }
    }
}

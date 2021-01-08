using System;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Equivalent to String.Format.
        /// </summary>
        /// <param name="format">The format string in String.Format style.</param>
        /// <param name="args">The arguments.</param>
        internal static string ToFormat(this string format, params object[] args)
            => string.Format(format, args);

        /// <summary>
        /// Returns a camel case version of the string.
        /// </summary>
        /// <param name="s">The source string.</param>
        /// <returns>System.String.</returns>
        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            var newFirstLetter = char.ToLowerInvariant(s[0]);
            if (newFirstLetter == s[0])
                return s;

            return s.Length <= 256
                ? FastChangeFirstLetter(newFirstLetter, s)
                : newFirstLetter + s.Substring(1);
        }

        /// <summary>
        /// Returns a pascal case version of the string.
        /// </summary>
        /// <param name="s">The source string.</param>
        /// <returns>System.String.</returns>
        public static string ToPascalCase(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            var newFirstLetter = char.ToUpperInvariant(s[0]);
            if (newFirstLetter == s[0])
                return s;

            return s.Length <= 256
               ? FastChangeFirstLetter(newFirstLetter, s)
               : newFirstLetter + s.Substring(1);
        }

        private static string FastChangeFirstLetter(char newFirstLetter, string s)
        {
            Span<char> buffer = stackalloc char[s.Length];
            buffer[0] = newFirstLetter;
            s.AsSpan().Slice(1).CopyTo(buffer.Slice(1));
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a constant case version of this string. For example, converts 'StringError' into 'STRING_ERROR'.
        /// </summary>
        public static string ToConstantCase(this string s)
        {
            //aka: return Regex.Replace(s, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToUpperInvariant();
            int i;
            int count = s.Length;
            for (i = 0; i < count - 2; ++i)
            {
                var c = s[i];
                if (char.IsLower(c) || char.IsDigit(c))
                {
                    if (char.IsUpper(s[i + 1]))
                    {
                        s = s.Substring(0, ++i) + '_' + s.Substring(i);
                        ++count;
                        continue;
                    }
                }
                if (char.IsUpper(c) && char.IsUpper(s[i + 1]))
                {
                    if (char.IsLower(s[i + 2]))
                    {
                        s = s.Substring(0, ++i) + '_' + s.Substring(i);
                        ++count;
                        continue;
                    }
                }
            }
            if (i < count - 1)
            {
                var c = s[i];
                if (char.IsLower(c) || char.IsDigit(c))
                {
                    if (char.IsUpper(s[i + 1]))
                    {
                        s = s.Substring(0, ++i) + '_' + s.Substring(i);
                    }
                }
            }

            return s.ToUpperInvariant();
        }

        private static readonly char[] _bangs = new char[] { '!', '[', ']' };

        /// <summary>
        /// Removes brackets and exclamation points from a GraphQL type name -- for example,
        /// converts <c>[Int!]</c> to <c>Int</c>
        /// </summary>
        public static string TrimGraphQLTypes(this string name) => name.Trim().Trim(_bangs);
    }
}

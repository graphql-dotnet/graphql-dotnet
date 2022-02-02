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
        internal static string ToFormat(this string format, params object?[] args) //TODO: remove in v5
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
        public static string ToConstantCase(this string value) //TODO: rewrite to stackalloc/ string.Create()
        {
            int i;
            int strLength = value.Length;
            // iterate through each character in the string, stopping a character short of the end
            for (i = 0; i < strLength - 1; ++i)
            {
                var curChar = value[i];
                var nextChar = value[i + 1];
                // look for the pattern [a-z][A-Z]
                if (char.IsLower(curChar) && char.IsUpper(nextChar))
                {
                    InsertUnderscore();
                    // then skip the remaining match checks since we already found a match here
                    continue;
                }
                // look for the pattern [0-9][A-Za-z]
                if (char.IsDigit(curChar) && char.IsLetter(nextChar))
                {
                    InsertUnderscore();
                    continue;
                }
                // look for the pattern [A-Za-z][0-9]
                if (char.IsLetter(curChar) && char.IsDigit(nextChar))
                {
                    InsertUnderscore();
                    continue;
                }
                // if there's enough characters left, look for the pattern [A-Z][A-Z][a-z]
                if (i < strLength - 2 && char.IsUpper(curChar) && char.IsUpper(nextChar) && char.IsLower(value[i + 2]))
                {
                    InsertUnderscore();
                    continue;
                }
            }
            // convert the resulting string to uppercase
            return value.ToUpperInvariant();

            void InsertUnderscore()
            {
                // add an underscore between the two characters, increment i to skip the underscore, and increase strLength because the string is longer now
                value = value.Substring(0, ++i) + '_' + value.Substring(i);
                ++strLength;
            }
        }

        private static readonly char[] _bangs = new char[] { '!', '[', ']' };

        /// <summary>
        /// Removes brackets and exclamation points from a GraphQL type name -- for example,
        /// converts <c>[Int!]</c> to <c>Int</c>
        /// </summary>
        public static string TrimGraphQLTypes(this string name) => name.Trim().Trim(_bangs);
    }
}

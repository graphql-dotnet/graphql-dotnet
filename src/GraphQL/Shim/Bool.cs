using System;

namespace GraphQL
{
    /// <summary>
    /// Adapter to unify usages of bool.Parse(ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Bool
    {
#if NETSTANDARD2_1
        public static bool Parse(ReadOnlySpan<char> value) => bool.Parse(value);
#else
        // copied from .NET Core sources
        public static bool Parse(ReadOnlySpan<char> value) =>
            TryParse(value, out bool result) ? result : throw new FormatException($"String was not recognized as a valid Boolean.");

        public static bool TryParse(ReadOnlySpan<char> value, out bool result)
        {
            if (IsTrueStringIgnoreCase(value))
            {
                result = true;
                return true;
            }

            if (IsFalseStringIgnoreCase(value))
            {
                result = false;
                return true;
            }

            // Special case: Trim whitespace as well as null characters.
            value = TrimWhiteSpaceAndNull(value);

            if (IsTrueStringIgnoreCase(value))
            {
                result = true;
                return true;
            }

            if (IsFalseStringIgnoreCase(value))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        private static bool IsTrueStringIgnoreCase(ReadOnlySpan<char> value)
        {
            return (value.Length == 4 &&
                    (value[0] == 't' || value[0] == 'T') &&
                    (value[1] == 'r' || value[1] == 'R') &&
                    (value[2] == 'u' || value[2] == 'U') &&
                    (value[3] == 'e' || value[3] == 'E'));
        }

        private static bool IsFalseStringIgnoreCase(ReadOnlySpan<char> value)
        {
            return (value.Length == 5 &&
                    (value[0] == 'f' || value[0] == 'F') &&
                    (value[1] == 'a' || value[1] == 'A') &&
                    (value[2] == 'l' || value[2] == 'L') &&
                    (value[3] == 's' || value[3] == 'S') &&
                    (value[4] == 'e' || value[4] == 'E'));
        }

        private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
        {
            const char nullChar = (char)0x0000;

            int start = 0;
            while (start < value.Length)
            {
                if (!char.IsWhiteSpace(value[start]) && value[start] != nullChar)
                {
                    break;
                }
                start++;
            }

            int end = value.Length - 1;
            while (end >= start)
            {
                if (!char.IsWhiteSpace(value[end]) && value[end] != nullChar)
                {
                    break;
                }
                end--;
            }

            return value.Slice(start, end - start + 1);
        }
#endif
    }
}

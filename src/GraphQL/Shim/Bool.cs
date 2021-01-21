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
            if (value.Length == 4 &&
                value[0] == 't' &&
                value[1] == 'r' &&
                value[2] == 'u' &&
                value[3] == 'e')
            {
                result = true;
                return true;
            }

            if (value.Length == 5 &&
                value[0] == 'f' &&
                value[1] == 'a' &&
                value[2] == 'l' &&
                value[3] == 's' &&
                value[4] == 'e')
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }
#endif
    }
}

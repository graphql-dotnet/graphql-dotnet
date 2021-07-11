#nullable enable

using System;
using System.Globalization;

namespace GraphQL
{
    /// <summary>
    /// Adapter to unify usages of int.TryParse(ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Int
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out int result)
            => int.TryParse(s, style, provider, out result);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out int result)
            => int.TryParse(s.ToString(), style, provider, out result);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of long.TryParse(ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Long
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out long result)
            => long.TryParse(s, style, provider, out result);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out long result)
            => long.TryParse(s.ToString(), style, provider, out result);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of decimal.TryParse(ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Decimal
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out decimal result)
            => decimal.TryParse(s, style, provider, out result);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out decimal result)
            => decimal.TryParse(s.ToString(), style, provider, out result);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of BigInteger.TryParse(ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class BigInt
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out System.Numerics.BigInteger result)
            => System.Numerics.BigInteger.TryParse(s, style, provider, out result);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out System.Numerics.BigInteger result)
            => System.Numerics.BigInteger.TryParse(s.ToString(), style, provider, out result);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of double.TryParse(ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Double
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out double result)
            => double.TryParse(s, style, provider, out result);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out double result)
            => double.TryParse(s.ToString(), style, provider, out result);
#endif
    }
}

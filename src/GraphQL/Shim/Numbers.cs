using System.Globalization;

namespace GraphQL
{
    /// <summary>
    /// Adapter to unify usages of int.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Int
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out int result)
            => int.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out int result)
            => int.TryParse(s, style, provider, out result);
        public static int Parse(ReadOnlySpan<char> s)
            => int.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static int Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => int.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out int result)
            => int.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out int result)
            => int.TryParse(s.ToString(), style, provider, out result);
        public static int Parse(ReadOnlySpan<char> s)
            => int.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static int Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => int.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of long.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Long
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out long result)
            => long.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out long result)
            => long.TryParse(s, style, provider, out result);
        public static long Parse(ReadOnlySpan<char> s)
            => long.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static long Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => long.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out long result)
            => long.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out long result)
            => long.TryParse(s.ToString(), style, provider, out result);
        public static long Parse(ReadOnlySpan<char> s)
            => long.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static long Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => long.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of decimal.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Decimal
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out decimal result)
            => decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out decimal result)
            => decimal.TryParse(s, style, provider, out result);
        public static decimal Parse(ReadOnlySpan<char> s)
            => decimal.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static decimal Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => decimal.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out decimal result)
            => decimal.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out decimal result)
            => decimal.TryParse(s.ToString(), style, provider, out result);
        public static decimal Parse(ReadOnlySpan<char> s)
            => decimal.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static decimal Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => decimal.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of BigInteger.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class BigInt
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out System.Numerics.BigInteger result)
          => System.Numerics.BigInteger.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out System.Numerics.BigInteger result)
            => System.Numerics.BigInteger.TryParse(s, style, provider, out result);
        public static System.Numerics.BigInteger Parse(ReadOnlySpan<char> s)
            => System.Numerics.BigInteger.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static System.Numerics.BigInteger Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => System.Numerics.BigInteger.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out System.Numerics.BigInteger result)
            => System.Numerics.BigInteger.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out System.Numerics.BigInteger result)
            => System.Numerics.BigInteger.TryParse(s.ToString(), style, provider, out result);
        public static System.Numerics.BigInteger Parse(ReadOnlySpan<char> s)
            => System.Numerics.BigInteger.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static System.Numerics.BigInteger Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => System.Numerics.BigInteger.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of double.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Double
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out double result)
           => double.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out double result)
            => double.TryParse(s, style, provider, out result);
        public static double Parse(ReadOnlySpan<char> s)
            => double.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static double Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => double.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out double result)
            => double.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out double result)
            => double.TryParse(s.ToString(), style, provider, out result);
        public static double Parse(ReadOnlySpan<char> s)
            => double.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static double Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => double.Parse(s.ToString(), style, provider);
#endif
    }
}

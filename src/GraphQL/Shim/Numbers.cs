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
    /// Adapter to unify usages of uint.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class UInt
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out uint result)
            => uint.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out uint result)
            => uint.TryParse(s, style, provider, out result);
        public static uint Parse(ReadOnlySpan<char> s)
            => uint.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static uint Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => uint.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out uint result)
            => uint.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out uint result)
            => uint.TryParse(s.ToString(), style, provider, out result);
        public static uint Parse(ReadOnlySpan<char> s)
            => uint.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static uint Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => uint.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of ushort.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class UShort
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out ushort result)
            => ushort.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out ushort result)
            => ushort.TryParse(s, style, provider, out result);
        public static ushort Parse(ReadOnlySpan<char> s)
            => ushort.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => ushort.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out ushort result)
            => ushort.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out ushort result)
            => ushort.TryParse(s.ToString(), style, provider, out result);
        public static ushort Parse(ReadOnlySpan<char> s)
            => ushort.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => ushort.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of short.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Short
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out short result)
            => short.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out short result)
            => short.TryParse(s, style, provider, out result);
        public static short Parse(ReadOnlySpan<char> s)
            => short.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static short Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => short.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out short result)
            => short.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out short result)
            => short.TryParse(s.ToString(), style, provider, out result);
        public static short Parse(ReadOnlySpan<char> s)
            => short.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static short Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => short.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of byte.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class Byte
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out byte result)
            => byte.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out byte result)
            => byte.TryParse(s, style, provider, out result);
        public static byte Parse(ReadOnlySpan<char> s)
            => byte.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static byte Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => byte.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out byte result)
            => byte.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out byte result)
            => byte.TryParse(s.ToString(), style, provider, out result);
        public static byte Parse(ReadOnlySpan<char> s)
            => byte.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static byte Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => byte.Parse(s.ToString(), style, provider);
#endif
    }

    /// <summary>
    /// Adapter to unify usages of sbyte.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class SByte
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out sbyte result)
            => sbyte.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out sbyte result)
            => sbyte.TryParse(s, style, provider, out result);
        public static sbyte Parse(ReadOnlySpan<char> s)
            => sbyte.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => sbyte.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out sbyte result)
            => sbyte.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out sbyte result)
            => sbyte.TryParse(s.ToString(), style, provider, out result);
        public static sbyte Parse(ReadOnlySpan<char> s)
            => sbyte.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => sbyte.Parse(s.ToString(), style, provider);
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
    /// Adapter to unify usages of ulong.[Parse|TryParse](ReadOnlySpan) for netstandard2.0 and netstandard2.1
    /// </summary>
    internal static class ULong
    {
#if NETSTANDARD2_1
        public static bool TryParse(ReadOnlySpan<char> s, out ulong result)
            => ulong.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out ulong result)
            => ulong.TryParse(s, style, provider, out result);
        public static ulong Parse(ReadOnlySpan<char> s)
            => ulong.Parse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static ulong Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => ulong.Parse(s, style, provider);
#else
        //TODO: copy from .NET Core sources
        public static bool TryParse(ReadOnlySpan<char> s, out ulong result)
            => ulong.TryParse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out ulong result)
            => ulong.TryParse(s.ToString(), style, provider, out result);
        public static ulong Parse(ReadOnlySpan<char> s)
            => ulong.Parse(s.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        public static ulong Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
            => ulong.Parse(s.ToString(), style, provider);
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

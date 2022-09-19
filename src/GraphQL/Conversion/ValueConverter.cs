using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;

namespace GraphQL
{
    /// <summary>
    /// This class provides value conversions between objects of different types.
    /// Conversions are registered in a static thread safe dictionary and are used for all schemas in the application.
    /// <br/><br/>
    /// Each ScalarGraphType calls <see cref="ConvertTo(object, Type)">ConvertTo</see> method to return correct value
    /// type from its <see cref=" GraphQL.Types.ScalarGraphType.ParseValue(object)">ParseValue</see> method.
    /// Also conversions may be useful in advanced <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)">GetArgument</see>
    /// use cases when deserialization from the values dictionary to the complex input argument is required.
    /// </summary>
    public static class ValueConverter
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>> _valueConversions = new();

        /// <summary>
        /// Register built-in conversions. This list is expected to grow over time.
        /// </summary>
        static ValueConverter()
        {
            Register<string, sbyte>(value => sbyte.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, byte>(value => byte.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, short>(value => short.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, ushort>(value => ushort.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, int>(value => int.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, uint>(value => uint.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, long>(value => long.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, ulong>(value => ulong.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, BigInteger>(value => BigInteger.Parse(value, NumberFormatInfo.InvariantInfo));
            Register<string, float>(value => float.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
            Register<string, double>(value => double.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
            Register<string, decimal>(value => decimal.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
#if NET6_0_OR_GREATER
            Register<string, DateOnly>(value => DateOnly.Parse(value, DateTimeFormatInfo.InvariantInfo));
            Register<string, TimeOnly>(value => TimeOnly.Parse(value, DateTimeFormatInfo.InvariantInfo));
#endif
            Register<string, DateTime>(value => DateTimeOffset.Parse(value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).UtcDateTime);
            Register<string, DateTimeOffset>(value => DateTimeOffset.Parse(value, DateTimeFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(bool), value =>
            {
                string stringValue = (string)value;
                if (string.CompareOrdinal(stringValue, "1") == 0)
                    return BoolBox.True;
                else if (string.CompareOrdinal(stringValue, "0") == 0)
                    return BoolBox.False;

                return Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo).Boxed();
            });
            Register<string, Guid>(value => Guid.Parse(value));
            Register<string, Uri>(value => new Uri(value));
            Register<string, byte[]>(value => Convert.FromBase64String(value)); // such a built-in conversion for string->byte[] seems useful

            Register<DateTime, DateTimeOffset>(value => value);
            Register<DateTimeOffset, DateTime>(value => value.UtcDateTime);
            Register<TimeSpan, long>(value => (long)value.TotalSeconds);

            Register<int, sbyte>(value => checked((sbyte)value));
            Register<int, byte>(value => checked((byte)value));
            Register<int, short>(value => checked((short)value));
            Register<int, ushort>(value => checked((ushort)value));
            Register(typeof(int), typeof(bool), value => Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo).Boxed());
            Register<int, uint>(value => checked((uint)value));
            Register<int, long>(value => value);
            Register<int, ulong>(value => checked((ulong)value));
            Register<int, BigInteger>(value => new BigInteger(value));
            Register<int, double>(value => value);
            Register<int, float>(value => value);
            Register<int, decimal>(value => value);
            Register<int, TimeSpan>(value => TimeSpan.FromSeconds(value));

            Register<long, sbyte>(value => checked((sbyte)value));
            Register<long, byte>(value => checked((byte)value));
            Register<long, short>(value => checked((short)value));
            Register<long, ushort>(value => checked((ushort)value));
            Register<long, int>(value => checked((int)value));
            Register<long, uint>(value => checked((uint)value));
            Register<long, ulong>(value => checked((ulong)value));
            Register<long, BigInteger>(value => new BigInteger(value));
            Register<long, double>(value => value);
            Register<long, float>(value => value);
            Register<long, decimal>(value => value);
            Register<long, TimeSpan>(value => TimeSpan.FromSeconds(value));

            Register<BigInteger, sbyte>(value => checked((sbyte)value));
            Register<BigInteger, byte>(value => checked((byte)value));
            Register<BigInteger, decimal>(value => checked((decimal)value));
            Register<BigInteger, double>(value => checked((double)value));
            Register<BigInteger, short>(value => checked((short)value));
            Register<BigInteger, long>(value => checked((long)value));
            Register<BigInteger, sbyte>(value => checked((sbyte)value));
            Register<BigInteger, ushort>(value => checked((ushort)value));
            Register<BigInteger, uint>(value => checked((uint)value));
            Register<BigInteger, ulong>(value => checked((ulong)value));
            Register<BigInteger, int>(value => checked((int)value));
            Register<BigInteger, float>(value => checked((float)value));

            Register<uint, sbyte>(value => checked((sbyte)value));
            Register<uint, byte>(value => checked((byte)value));
            Register<uint, int>(value => checked((int)value));
            Register<uint, long>(value => value);
            Register<uint, ulong>(value => value);
            Register<uint, short>(value => checked((short)value));
            Register<uint, ushort>(value => checked((ushort)value));
            Register<uint, BigInteger>(value => new BigInteger(value));

            Register<ulong, BigInteger>(value => new BigInteger(value));

            Register<byte, sbyte>(value => checked((sbyte)value));
            Register<byte, int>(value => value);
            Register<byte, long>(value => value);
            Register<byte, ulong>(value => value);
            Register<byte, short>(value => value);
            Register<byte, ushort>(value => value);
            Register<byte, BigInteger>(value => new BigInteger(value));

            Register<sbyte, byte>(value => checked((byte)value));
            Register<sbyte, int>(value => value);
            Register<sbyte, long>(value => value);
            Register<sbyte, ulong>(value => checked((ulong)value));
            Register<sbyte, short>(value => value);
            Register<sbyte, ushort>(value => checked((ushort)value));
            Register<sbyte, BigInteger>(value => new BigInteger(value));

            Register<float, double>(value => value);
            Register<float, decimal>(value => checked((decimal)value));
            Register<float, BigInteger>(value => new BigInteger(value));

            Register<double, float>(value => checked((float)value));
            Register<double, decimal>(value => checked((decimal)value));

            Register<char, byte>(value => checked((byte)value));
            Register<char, int>(value => value);

            Register<decimal, double>(value => checked((double)value));
        }

        /// <summary>
        /// <para>Returns an object of the specified type and whose value is equivalent to the specified object.</para>
        /// <para>Throws a <see cref="InvalidOperationException"/> if there is no conversion registered; conversion functions may throw other exceptions</para>
        /// </summary>
        public static T? ConvertTo<T>(object? value)
        {
            object? v = ConvertTo(value, typeof(T));

            return v == null ? default : (T)v;
        }

        /// <summary>
        /// <para>Returns an object of the specified type and whose value is equivalent to the specified object.</para>
        /// <para>Throws a <see cref="InvalidOperationException"/> if there is no conversion registered; conversion functions may throw other exceptions</para>
        /// </summary>
        public static object? ConvertTo(object? value, Type targetType)
        {
            if (value == null)
                return null;

            if (!TryConvertTo(value, targetType, out object? result))
                throw new InvalidOperationException($"Could not find conversion from '{value.GetType().FullName}' to '{targetType.FullName}'");

            return result;
        }

        /// <summary>
        /// <para>
        /// If a conversion delegate was registered, converts an object to the specified type and
        /// returns <see langword="true"/>; returns <see langword="false"/> if no conversion delegate is registered.
        /// </para>
        /// <para>Conversion delegates may throw exceptions if the conversion was unsuccessful</para>
        /// </summary>
        internal static bool TryConvertTo(object? value, Type targetType, out object? result, Type? sourceType = null)
        {
            if (value == null || targetType.IsInstanceOfType(value))
            {
                result = value;
                return true;
            }

            var conversion = GetConversion(sourceType ?? value.GetType(), targetType);
            if (conversion == null)
            {
                result = null;
                return false;
            }
            else
            {
                result = conversion(value);
                return true;
            }
        }

        private static Func<object, object>? GetConversion(Type valueType, Type targetType)
        {
            return _valueConversions.TryGetValue(valueType, out var conversions) && conversions.TryGetValue(targetType, out var conversion)
                ? conversion
                : null;
        }

        /// <summary>
        /// Allows you to register your own conversion delegate from one type to another.
        /// <br/><br/>
        /// If the conversion from valueType to targetType is already registered, then it will be overwritten.
        /// </summary>
        /// <param name="valueType">Type of original value.</param>
        /// <param name="targetType">Converted value type.</param>
        /// <param name="conversion">Conversion delegate; <see langword="null"/> for unregister already registered conversion.</param>
        public static void Register(Type valueType, Type targetType, Func<object, object>? conversion)
        {
            if (!_valueConversions.TryGetValue(valueType, out var conversions) &&
                !_valueConversions.TryAdd(valueType, conversions = new ConcurrentDictionary<Type, Func<object, object>>()))
                conversions = _valueConversions[valueType];

            if (conversion == null)
                conversions.TryRemove(targetType, out var _);
            else
                conversions[targetType] = conversion;
        }

        /// <summary>
        /// Allows you to register your own conversion delegate from one type to another.
        /// <br/><br/>
        /// If the conversion from TSource to TTarget is already registered, then it will be overwritten.
        /// </summary>
        /// <typeparam name="TSource">Type of original value.</typeparam>
        /// <typeparam name="TTarget">Converted value type.</typeparam>
        /// <param name="conversion">Conversion delegate; <see langword="null"/> for unregister already registered conversion.</param>
        public static void Register<TSource, TTarget>(Func<TSource, TTarget>? conversion)
            => Register(typeof(TSource), typeof(TTarget), conversion == null ? null : v => conversion((TSource)v)!);

        /// <summary>
        /// Allows you to register your own conversion delegate from dictionary to some complex object.
        /// <br/><br/>
        /// This method may be useful in advanced <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)">GetArgument</see>
        /// use cases when deserialization from the values dictionary to the complex input argument is required.
        /// <br/><br/>
        /// If the conversion from dictionary to TTarget is already registered, then it will be overwritten.
        /// </summary>
        /// <typeparam name="TTarget">Converted value type.</typeparam>
        /// <param name="conversion">Conversion delegate; <see langword="null"/> for unregister already registered conversion.</param>
        public static void Register<TTarget>(Func<IDictionary<string, object>, TTarget>? conversion)
            where TTarget : class
            => Register<IDictionary<string, object>, TTarget>(conversion);
    }
}

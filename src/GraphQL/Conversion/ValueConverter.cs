using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace GraphQL
{
    public static class ValueConverter
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>> _valueConversions
            = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>>();

        private static readonly ConcurrentDictionary<Type, Func<IDictionary<string, object>, object>> _objectConversions
            = new ConcurrentDictionary<Type, Func<IDictionary<string, object>, object>>();

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

            Register<DateTime, DateTimeOffset>(value => value);
            Register<DateTimeOffset, DateTime>(value => value.UtcDateTime);
            Register<TimeSpan, long>(value => (long)value.TotalSeconds);

            Register<int, sbyte>(value => Convert.ToSByte(value, NumberFormatInfo.InvariantInfo));
            Register<int, byte>(value => Convert.ToByte(value, NumberFormatInfo.InvariantInfo));
            Register<int, short>(value => Convert.ToInt16(value, NumberFormatInfo.InvariantInfo));
            Register<int, ushort>(value => Convert.ToUInt16(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(bool), value => Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo).Boxed());
            Register<int, uint>(value => Convert.ToUInt32(value, NumberFormatInfo.InvariantInfo));
            Register<int, long>(value => value);
            Register<int, ulong>(value => Convert.ToUInt64(value, NumberFormatInfo.InvariantInfo));
            Register<int, BigInteger>(value => new BigInteger(value));
            Register<int, double>(value => Convert.ToDouble(value, NumberFormatInfo.InvariantInfo));
            Register<int, decimal>(value => Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo));
            Register<int, TimeSpan>(value => TimeSpan.FromSeconds(value));

            Register<long, sbyte>(value => Convert.ToSByte(value, NumberFormatInfo.InvariantInfo));
            Register<long, byte>(value => Convert.ToByte(value, NumberFormatInfo.InvariantInfo));
            Register<long, short>(value => Convert.ToInt16(value, NumberFormatInfo.InvariantInfo));
            Register<long, ushort>(value => Convert.ToUInt16(value, NumberFormatInfo.InvariantInfo));
            Register<long, int>(value => Convert.ToInt32(value, NumberFormatInfo.InvariantInfo));
            Register<long, uint>(value => Convert.ToUInt32(value, NumberFormatInfo.InvariantInfo));
            Register<long, ulong>(value => Convert.ToUInt64(value, NumberFormatInfo.InvariantInfo));
            Register<long, BigInteger>(value => new BigInteger(value));
            Register<long, double>(value => value);
            Register<long, decimal>(value => value);
            Register<long, TimeSpan>(value => TimeSpan.FromSeconds(value));

            Register<BigInteger, sbyte>(value => (sbyte)value);
            Register<BigInteger, byte>(value => (byte)value);
            Register<BigInteger, decimal>(value => (decimal)value);
            Register<BigInteger, double>(value => (double)value);
            Register<BigInteger, short>(value => (short)value);
            Register<BigInteger, long>(value => (long)value);
            Register<BigInteger, sbyte>(value => (sbyte)value);
            Register<BigInteger, ushort>(value => (ushort)value);
            Register<BigInteger, uint>(value => (uint)value);
            Register<BigInteger, ulong>(value => (ulong)value);
            Register<BigInteger, int>(value => (int)value);
            Register<BigInteger, float>(value => (float)value);

            Register<uint, sbyte>(value => (sbyte)value);
            Register<uint, byte>(value => (byte)value);
            Register<uint, int>(value => (int)value);
            Register<uint, long>(value => value);
            Register<uint, ulong>(value => value);
            Register<uint, short>(value => (short)value);
            Register<uint, ushort>(value => (ushort)value);
            Register<uint, BigInteger>(value => new BigInteger(value));

            Register<ulong, BigInteger>(value => new BigInteger(value));

            Register<byte, sbyte>(value => (sbyte)value);
            Register<byte, int>(value => value);
            Register<byte, long>(value => value);
            Register<byte, ulong>(value => value);
            Register<byte, short>(value => value);
            Register<byte, ushort>(value => value);
            Register<byte, BigInteger>(value => new BigInteger(value));

            Register<sbyte, byte>(value => (byte)value);
            Register<sbyte, int>(value => value);
            Register<sbyte, long>(value => value);
            Register<sbyte, ulong>(value => (ulong)value);
            Register<sbyte, short>(value => value);
            Register<sbyte, ushort>(value => (ushort)value);
            Register<sbyte, BigInteger>(value => new BigInteger(value));

            Register<float, double>(value => (double)value);
            Register<float, decimal>(value => Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo));
            Register<float, BigInteger>(value => new BigInteger(value));

            Register<double, decimal>(value => Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo));

            Register<string, Uri>(value => new Uri(value));
            // registering such a default conversion for string->byte[] seems useful
            Register<string, byte[]>(value => Convert.FromBase64String(value));

            Register<char, byte>(value => Convert.ToByte(value));
            Register<char, int>(value => Convert.ToInt32(value));
        }

        public static T ConvertTo<T>(object value)
        {
            object v = ConvertTo(value, typeof(T));

            return v == null ? default : (T)v;
        }

        public static object ConvertTo(object value, Type targetType)
        {
            if (!TryConvertTo(value, targetType, out object result))
                throw new InvalidOperationException($"Could not find conversion from '{value.GetType().FullName}' to '{targetType.FullName}'");

            return result;
        }

        public static bool TryConvertTo<T>(object value, out T result)
        {
            if (TryConvertTo(value, typeof(T), out object v))
            {
                result = (T)v;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static bool TryConvertTo(object value, Type targetType, out object result)
        {
            if (value == null || targetType.IsInstanceOfType(value))
            {
                result = value;
                return true;
            }

            var conversion = GetConversion(value.GetType(), targetType);
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

        internal static bool TryConvertToObject(IDictionary<string, object> value, Type targetType, out object result)
        {
            if (_objectConversions.TryGetValue(targetType, out var conversion))
            {
                result = conversion(value);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private static Func<object, object> GetConversion(Type valueType, Type targetType)
        {
            return _valueConversions.TryGetValue(valueType, out var conversions) && conversions.TryGetValue(targetType, out var conversion)
                ? conversion
                : null;
        }

        /// <summary>
        /// Allows you to register your own conversion method from one type to another.
        /// If the conversion from valueType to targetType is already registered, then it will be overwritten.
        /// </summary>
        /// <param name="valueType">Type of original value.</param>
        /// <param name="targetType">Converted value type. </param>
        /// <param name="conversion">Conversion delegate; <c>null</c> for unregister already registered conversion.</param>
        public static void Register(Type valueType, Type targetType, Func<object, object> conversion)
        {
            if (!_valueConversions.TryGetValue(valueType, out var conversions))
                if (!_valueConversions.TryAdd(valueType, conversions = new ConcurrentDictionary<Type, Func<object, object>>()))
                    conversions = _valueConversions[valueType];

            if (conversion == null)
                conversions.TryRemove(targetType, out var _);
            else
                conversions[targetType] = conversion;
        }

        public static void Register<TSource, TTarget>(Func<TSource, TTarget> conversion)
            => Register(typeof(TSource), typeof(TTarget), v => conversion((TSource)v));

        public static void RegisterObject<TTarget>(Func<IDictionary<string, object>, TTarget> conversion)
            where TTarget : class
        {
            if (conversion == null)
                _objectConversions.TryRemove(typeof(TTarget), out var _);
            else
                _objectConversions[typeof(TTarget)] = conversion;
        }
    }
}

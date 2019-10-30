using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace GraphQL
{
    public static class ValueConverter
    {
        private static readonly Dictionary<Type, Dictionary<Type, Func<object, object>>> _valueConversions
            = new Dictionary<Type, Dictionary<Type, Func<object, object>>>();

        static ValueConverter()
        {
            Register(typeof(string), typeof(sbyte), value => sbyte.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(byte), value => byte.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(short), value => short.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(ushort), value => ushort.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(int), value => int.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(uint), value => uint.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(long), value => long.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(ulong), value => ulong.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(BigInteger), value => BigInteger.Parse((string)value, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(float), value => float.Parse((string)value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(double), value => double.Parse((string)value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(decimal), value => decimal.Parse((string)value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(DateTime), value => DateTimeOffset.Parse((string)value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).UtcDateTime);
            Register(typeof(string), typeof(DateTimeOffset), value => DateTimeOffset.Parse((string)value, DateTimeFormatInfo.InvariantInfo));
            Register(typeof(string), typeof(bool), value =>
            {
                string stringValue = (string)value;
                if (string.CompareOrdinal(stringValue, "1") == 0)
                    return true;
                else if (string.CompareOrdinal(stringValue, "0") == 0)
                    return false;

                return Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo);
            });
            Register(typeof(string), typeof(Guid), value => Guid.Parse((string)value));

            Register(typeof(DateTime), typeof(DateTimeOffset), value => (DateTimeOffset)(DateTime)value);
            Register(typeof(DateTimeOffset), typeof(DateTime), value => ((DateTimeOffset)value).UtcDateTime);
            Register(typeof(TimeSpan), typeof(long), value => ((TimeSpan)value).TotalSeconds);

            Register(typeof(int), typeof(sbyte), value => Convert.ToSByte(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(byte), value => Convert.ToByte(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(short), value => Convert.ToInt16(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(ushort), value => Convert.ToUInt16(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(bool), value => Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(uint), value => Convert.ToUInt32(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(long), value => (long)(int)value);
            Register(typeof(int), typeof(ulong), value => Convert.ToUInt64(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(BigInteger), value => new BigInteger((int)value));
            Register(typeof(int), typeof(double), value => Convert.ToDouble(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(decimal), value => Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(int), typeof(TimeSpan), value => TimeSpan.FromSeconds((int)value));

            Register(typeof(long), typeof(sbyte), value => Convert.ToSByte(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(long), typeof(byte), value => Convert.ToByte(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(long), typeof(short), value => Convert.ToInt16(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(long), typeof(ushort), value => Convert.ToUInt16(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(long), typeof(int), value => Convert.ToInt32(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(long), typeof(uint), value => Convert.ToUInt32(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(long), typeof(ulong), value => Convert.ToUInt64(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(long), typeof(BigInteger), value => new BigInteger((long)value));
            Register(typeof(long), typeof(double), value => (double)(long)value);
            Register(typeof(long), typeof(decimal), value => (decimal)(long)value);
            Register(typeof(long), typeof(TimeSpan), value => TimeSpan.FromSeconds((long)value));

            Register(typeof(BigInteger), typeof(sbyte), value => (sbyte)(BigInteger)value);
            Register(typeof(BigInteger), typeof(byte), value => (byte)(BigInteger)value);
            Register(typeof(BigInteger), typeof(decimal), value => (decimal)(BigInteger)value);
            Register(typeof(BigInteger), typeof(double), value => (double)(BigInteger)value);
            Register(typeof(BigInteger), typeof(short), value => (short)(BigInteger)value);
            Register(typeof(BigInteger), typeof(long), value => (long)(BigInteger)value);
            Register(typeof(BigInteger), typeof(sbyte), value => (sbyte)(BigInteger)value);
            Register(typeof(BigInteger), typeof(ushort), value => (ushort)(BigInteger)value);
            Register(typeof(BigInteger), typeof(uint), value => (uint)(BigInteger)value);
            Register(typeof(BigInteger), typeof(ulong), value => (ulong)(BigInteger)value);
            Register(typeof(BigInteger), typeof(int), value => (int)(BigInteger)value);
            Register(typeof(BigInteger), typeof(float), value => (float)(BigInteger)value);

            Register(typeof(uint), typeof(sbyte), value => (sbyte)(uint)value);
            Register(typeof(uint), typeof(byte), value => (byte)(uint)value);
            Register(typeof(uint), typeof(int), value => (int)(uint)value);
            Register(typeof(uint), typeof(long), value => (long)(uint)value);
            Register(typeof(uint), typeof(ulong), value => (ulong)(uint)value);
            Register(typeof(uint), typeof(short), value => (short)(uint)value);
            Register(typeof(uint), typeof(ushort), value => (ushort)(uint)value);
            Register(typeof(uint), typeof(BigInteger), value => new BigInteger((uint)value));

            Register(typeof(ulong), typeof(BigInteger), value => new BigInteger((ulong)value));

            Register(typeof(byte), typeof(sbyte), value => (sbyte)(byte)value);
            Register(typeof(byte), typeof(int), value => (int)(byte)value);
            Register(typeof(byte), typeof(long), value => (long)(byte)value);
            Register(typeof(byte), typeof(ulong), value => (ulong)(byte)value);
            Register(typeof(byte), typeof(short), value => (short)(byte)value);
            Register(typeof(byte), typeof(ushort), value => (ushort)(byte)value);
            Register(typeof(byte), typeof(BigInteger), value => new BigInteger((byte)value));

            Register(typeof(sbyte), typeof(byte), value => (byte)(sbyte)value);
            Register(typeof(sbyte), typeof(int), value => (int)(sbyte)value);
            Register(typeof(sbyte), typeof(long), value => (long)(sbyte)value);
            Register(typeof(sbyte), typeof(ulong), value => (ulong)(sbyte)value);
            Register(typeof(sbyte), typeof(short), value => (short)(sbyte)value);
            Register(typeof(sbyte), typeof(ushort), value => (ushort)(sbyte)value);
            Register(typeof(sbyte), typeof(BigInteger), value => new BigInteger((sbyte)value));

            Register(typeof(float), typeof(double), value => (double)(float)value);
            Register(typeof(float), typeof(decimal), value => Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo));
            Register(typeof(float), typeof(BigInteger), value => new BigInteger((float)value));

            Register(typeof(double), typeof(decimal), value => Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo));

            Register(typeof(string), typeof(Uri), value => value is string s ? new Uri(s) : (Uri)value);
        }

        public static object ConvertTo(object value, Type targetType)
        {
            if (value == null || targetType.IsInstanceOfType(value))
                return value;

            var conversion = GetConversion(value.GetType(), targetType);
            return conversion(value);
        }

        public static T ConvertTo<T>(object value)
        {
            var v = ConvertTo(value, typeof(T));

            return v == null ? default : (T)v;
        }

        private static Func<object, object> GetConversion(Type valueType, Type targetType)
        {
            if (_valueConversions.TryGetValue(valueType, out var conversions) && conversions.TryGetValue(targetType, out var conversion))
                return conversion;

            throw new InvalidOperationException($"Could not find conversion from '{valueType.FullName}' to '{targetType.FullName}'");
        }

        public static void Register(Type valueType, Type targetType, Func<object, object> conversion)
        {
            if (conversion == null)
                throw new ArgumentNullException(nameof(conversion));

            if (!_valueConversions.TryGetValue(valueType, out var conversions))
                _valueConversions.Add(valueType, conversions = new Dictionary<Type, Func<object, object>>());

            conversions[targetType] = conversion;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace GraphQL
{
    public static class ValueConverter
    {
        private static readonly Dictionary<Type, Dictionary<Type, Func<object, object>>> ValueConversions
            = new Dictionary<Type, Dictionary<Type, Func<object, object>>>();

        static ValueConverter()
        {
            Register(typeof(string), typeof(short), ParseShort);
            Register(typeof(string), typeof(ushort), ParseUShort);
            Register(typeof(string), typeof(int), ParseInt);
            Register(typeof(string), typeof(uint), ParseUInt);
            Register(typeof(string), typeof(long), ParseLong);
            Register(typeof(string), typeof(ulong), ParseULong);
            Register(typeof(string), typeof(float), ParseFloat);
            Register(typeof(string), typeof(double), ParseDouble);
            Register(typeof(string), typeof(decimal), ParseDecimal);
            Register(typeof(string), typeof(DateTime), ParseDateTime);
            Register(typeof(string), typeof(DateTimeOffset), ParseDateTimeOffset);
            Register(typeof(string), typeof(bool), ParseBool);
            Register(typeof(string), typeof(Guid), ParseGuid);

            Register(typeof(DateTime), typeof(DateTimeOffset), DateTimeToDateTimeOffset);
            Register(typeof(DateTimeOffset), typeof(DateTime), DateTimeOffsetToDateTime);
            Register(typeof(TimeSpan), typeof(long), TimeSpanToLong);

            Register(typeof(int), typeof(short), IntToShort);
            Register(typeof(int), typeof(ushort), IntToUShort);
            Register(typeof(int), typeof(bool), IntToBool);
            Register(typeof(int), typeof(uint), IntToUInt);
            Register(typeof(int), typeof(long), IntToLong);
            Register(typeof(int), typeof(ulong), IntToULong);
            Register(typeof(int), typeof(double), IntToDouble);
            Register(typeof(int), typeof(decimal), IntToDecimal);
            Register(typeof(int), typeof(TimeSpan), IntToTimeSpan);

            Register(typeof(long), typeof(short), LongToShort);
            Register(typeof(long), typeof(ushort), LongToUShort);
            Register(typeof(long), typeof(int), LongToInt);
            Register(typeof(long), typeof(uint), LongToUInt);
            Register(typeof(long), typeof(ulong), LongToULong);
            Register(typeof(long), typeof(double), LongToDouble);
            Register(typeof(long), typeof(decimal), LongToDecimal);
            Register(typeof(long), typeof(TimeSpan), LongToTimeSpan);

            Register(typeof(uint), typeof(int), UInt32ToInt32);
            Register(typeof(uint), typeof(long), UInt32ToLong);
            Register(typeof(uint), typeof(ulong), UInt32ToULong);
            Register(typeof(uint), typeof(short), UInt32ToShort);
            Register(typeof(uint), typeof(ushort), UInt32ToUShort);

            Register(typeof(byte), typeof(int), ByteToInt32);
            Register(typeof(byte), typeof(long), ByteToLong);
            Register(typeof(byte), typeof(ulong), ByteToULong);
            Register(typeof(byte), typeof(short), ByteToShort);
            Register(typeof(byte), typeof(ushort), ByteToUShort);

            Register(typeof(sbyte), typeof(int), SByteToInt32);
            Register(typeof(sbyte), typeof(long), SByteToLong);
            Register(typeof(sbyte), typeof(ulong), SByteToULong);
            Register(typeof(sbyte), typeof(short), SByteToShort);
            Register(typeof(sbyte), typeof(ushort), SByteToUShort);

            Register(typeof(float), typeof(double), FloatToDouble);
            Register(typeof(float), typeof(decimal), FloatToDecimal);

            Register(typeof(double), typeof(decimal), DoubleToDecimal);

            Register(typeof(string), typeof(Uri), ParseUri);
        }

        private static object SByteToInt32(object value) => (int)(sbyte)value;

        private static object SByteToLong(object value) => (long)(sbyte)value;

        private static object SByteToULong(object value) => (ulong)(sbyte)value;

        private static object SByteToShort(object value) => (short)(sbyte)value;

        private static object SByteToUShort(object value) => (ushort)(sbyte)value;

        private static object ByteToInt32(object value) => (int)(byte)value;

        private static object ByteToLong(object value) => (long)(byte)value;

        private static object ByteToULong(object value) => (ulong)(byte)value;

        private static object ByteToShort(object value) => (short)(byte)value;

        private static object ByteToUShort(object value) => (ushort)(byte)value;

        private static object UInt32ToInt32(object value) => (int)(uint)value;

        private static object UInt32ToLong(object value) => (long)(uint)value;

        private static object UInt32ToULong(object value) => (ulong)(uint)value;

        private static object UInt32ToShort(object value) => (short)(uint)value;

        private static object UInt32ToUShort(object value) => (ushort)(uint)value;

        private static object IntToDouble(object value)
        {
            var intValue = (int)value;
            return Convert.ToDouble(intValue, NumberFormatInfo.InvariantInfo);
        }

        private static object IntToDecimal(object value)
        {
            var intValue = (int)value;
            return Convert.ToDecimal(intValue, NumberFormatInfo.InvariantInfo);
        }

        private static object FloatToDouble(object value) => (double)(float)value;

        private static object FloatToDecimal(object value)
        {
            var floatValue = (float)value;
            return Convert.ToDecimal(floatValue, NumberFormatInfo.InvariantInfo);
        }

        private static object DoubleToDecimal(object value)
        {
            var doubleValue = (double)value;
            return Convert.ToDecimal(doubleValue, NumberFormatInfo.InvariantInfo);
        }

        private static object LongToDouble(object value) => (double)(long)value;

        private static object LongToDecimal(object value) => (decimal)(long)value;

        private static object LongToInt(object value)
        {
            var longValue = (long)value;
            return Convert.ToInt32(longValue, NumberFormatInfo.InvariantInfo);
        }

        private static object IntToLong(object value) => (long)(int)value;

        private static object ParseGuid(object value) => Guid.Parse((string)value);

        private static object ParseBool(object value)
        {
            string stringValue = (string)value;
            if (string.CompareOrdinal(stringValue, "1") == 0)
                return true;
            else if (string.CompareOrdinal(stringValue, "0") == 0)
                return false;

            return Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo);
        }

        private static object IntToBool(object value)
        {
            return Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo);
        }

        private static object DateTimeOffsetToDateTime(object value)
        {
            var dateTimeOffset = (DateTimeOffset)value;
            return dateTimeOffset.UtcDateTime;
        }

        private static object DateTimeToDateTimeOffset(object value)
        {
            var dateTime = (DateTime)value;
            return (DateTimeOffset)dateTime;
        }

        private static object IntToTimeSpan(object value) => TimeSpan.FromSeconds((int)value);

        private static object LongToTimeSpan(object value) => TimeSpan.FromSeconds((long)value);

        private static object TimeSpanToLong(object value) => ((TimeSpan)value).TotalSeconds;

        private static object ParseDateTimeOffset(object value)
        {
            var stringValue = (string)value;
            return DateTimeOffset.Parse(
                stringValue,
                DateTimeFormatInfo.InvariantInfo);
        }

        private static object ParseDateTime(object value)
        {
            var stringValue = (string)value;
            return DateTimeOffset.Parse(
                stringValue,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).UtcDateTime;
        }

        private static object ParseLong(object value)
        {
            return Convert.ToInt64(value, NumberFormatInfo.InvariantInfo);
        }

        private static object ParseDouble(object value)
        {
            var v = double.Parse(
                (string)value,
                NumberStyles.Float,
                NumberFormatInfo.InvariantInfo);

            return v;
        }

        private static object ParseDecimal(object value)
        {
            return decimal.Parse(
                (string)value,
                NumberStyles.Float,
                NumberFormatInfo.InvariantInfo);
        }

        private static object ParseFloat(object value)
        {
            return float.Parse(
                (string)value,
                NumberStyles.Float,
                NumberFormatInfo.InvariantInfo);
        }

        private static object ParseInt(object value)
        {
            return Convert.ToInt32(value, NumberFormatInfo.InvariantInfo);
        }

        private static object ParseUri(object value) =>
            value is string s
                ? new Uri(s)
                : (Uri)value;

        private static object ParseShort(object value) => convertToInt16((string)value);
        private static object IntToShort(object value) => convertToInt16((int)value);
        private static object LongToShort(object value) => convertToInt16((long)value);

        private static object convertToInt16<T>(T value) =>
            Convert.ToInt16(value, NumberFormatInfo.InvariantInfo);

        private static object ParseUShort(object value) => convertToUInt16(value);
        private static object IntToUShort(object value) => convertToUInt16((int)value);
        private static object LongToUShort(object value) => convertToUInt16((long)value);

        private static object convertToUInt16<T>(T value) =>
            Convert.ToUInt16(value, NumberFormatInfo.InvariantInfo);

        private static object ParseUInt(object value) => convertToUInt32(value);
        private static object IntToUInt(object value) => convertToUInt32((int)value);
        private static object LongToUInt(object value) => convertToUInt32((long)value);

        private static object convertToUInt32<T>(T value) =>
            Convert.ToUInt32(value, NumberFormatInfo.InvariantInfo);

        private static object ParseULong(object value) => convertToUInt64(value);
        private static object IntToULong(object value) => convertToUInt64((int)value);
        private static object LongToULong(object value) => convertToUInt64((long)value);

        private static object convertToUInt64<T>(T value) =>
            Convert.ToUInt64(value, NumberFormatInfo.InvariantInfo);

        public static object ConvertTo(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsInstanceOfType(value)) return value;

            var valueType = value.GetType();
            var conversion = GetConversion(valueType, targetType);
            return conversion(value);
        }

        public static T ConvertTo<T>(object value)
        {
            var v = ConvertTo(value, typeof(T));

            return v == null ? default : (T)v;
        }

        private static Func<object, object> GetConversion(Type valueType, Type targetType)
        {
            if (ValueConversions.TryGetValue(valueType, out var conversions) && conversions.TryGetValue(targetType, out var conversion))
                return conversion;

            throw new InvalidOperationException($"Could not find conversion from {valueType.FullName} to {targetType.FullName}");
        }

        public static void Register(Type valueType, Type targetType, Func<object, object> conversion)
        {
            if (!ValueConversions.TryGetValue(valueType, out var conversions))
                ValueConversions.Add(valueType, conversions = new Dictionary<Type, Func<object, object>>());

            conversions[targetType] = conversion;
        }
    }
}

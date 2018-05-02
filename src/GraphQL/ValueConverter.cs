using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace GraphQL
{
    public static class ValueConverter
    {
        private static readonly Dictionary<Type, Dictionary<Type, Func<object, object>>> ValueConversions
            = new Dictionary<Type, Dictionary<Type, Func<object, object>>>();

        static ValueConverter()
        {
            Register(typeof(string), typeof(int), ParseInt);
            Register(typeof(string), typeof(long), ParseLong);
            Register(typeof(string), typeof(float), ParseFloat);
            Register(typeof(string), typeof(double), ParseDouble);
            Register(typeof(string), typeof(decimal), ParseDecimal);
            Register(typeof(string), typeof(DateTime), ParseDateTime);
            Register(typeof(string), typeof(DateTimeOffset), ParseDateTimeOffset);
            Register(typeof(string), typeof(bool), ParseBool);
            Register(typeof(string), typeof(Guid), ParseGuid);

            Register(typeof(DateTime), typeof(DateTimeOffset), DateTimeToDateTimeOffset);
            Register(typeof(DateTimeOffset), typeof(DateTime), DateTimeOffsetToDateTime);

            Register(typeof(int), typeof(bool), IntToBool);
            Register(typeof(int), typeof(long), IntToLong);
            Register(typeof(int), typeof(decimal), IntToDecimal);

            Register(typeof(long), typeof(int), LongToInt);

            Register(typeof(double), typeof(decimal), DoubleToDecimal);
        }

        private static object IntToDecimal(object value)
        {
            var intValue = (int) value;
            return Convert.ToDecimal(intValue, NumberFormatInfo.InvariantInfo);
        }

        private static object DoubleToDecimal(object value)
        {
            var doubleValue = (double) value;
            return Convert.ToDecimal(doubleValue, NumberFormatInfo.InvariantInfo);
        }

        private static object LongToInt(object value)
        {
            var longValue = (long) value;
            return (int) longValue;
        }

        private static object IntToLong(object value)
        {
            var intValue = (int) value;
            return (long) intValue;
        }

        private static object ParseGuid(object value)
        {
            var stringValue = (string) value;
            return Guid.Parse(stringValue);
        }

        private static object ParseBool(object value)
        {
            return Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo);
        }

        private static object IntToBool(object value)
        {
            return Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo);
        }

        private static object DateTimeOffsetToDateTime(object value)
        {
            var dateTimeOffset = (DateTimeOffset) value;
            return dateTimeOffset.UtcDateTime;
        }

        private static object DateTimeToDateTimeOffset(object value)
        {
            var dateTime = (DateTime) value;
            return new DateTimeOffset(dateTime, TimeSpan.Zero);
        }

        private static object ParseDateTimeOffset(object value)
        {
            var stringValue = (string) value;
            return DateTimeOffset.Parse(
                stringValue,
                DateTimeFormatInfo.InvariantInfo);
        }

        private static object ParseDateTime(object value)
        {
            var stringValue = (string) value;
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
            var v =  double.Parse(
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

            if (v == null)
                return default(T);

            return (T) v;
        }

        private static Func<object, object> GetConversion(Type valueType, Type targetType)
        {
            if (ValueConversions.TryGetValue(valueType, out var conversions))
                if (conversions.TryGetValue(targetType, out var conversion))
                    return conversion;

            throw new InvalidOperationException(
                $"Could not find conversion from {valueType.FullName} to {targetType.FullName}");
        }

        public static void Register(Type valueType, Type targetType, Func<object, object> conversion)
        {
            if (!ValueConversions.ContainsKey(valueType))
                ValueConversions.Add(valueType, new Dictionary<Type, Func<object, object>>());

            var conversions = ValueConversions[valueType];
            conversions[targetType] = conversion;
        }
    }
}

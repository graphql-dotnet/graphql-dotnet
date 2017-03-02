using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Conversion;

namespace GraphQL
{
    public static class ObjectExtensions
    {
        private static readonly Lazy<Conversions> _conversions = new Lazy<Conversions>(() => new Conversions());

        public static T ToObject<T>(this IDictionary<string, object> source)
            where T : class, new()
        {
            return (T) ToObject(source, typeof(T));
        }

        public static object ToObject(this IDictionary<string, object> source, Type type)
        {
            var obj = Activator.CreateInstance(type);

            foreach (var item in source)
            {
                var propertyType = type.GetProperty(item.Key,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyType != null)
                {
                    var value = GetPropertyValue(item.Value, propertyType.PropertyType);
                    propertyType.SetValue(obj, value, null);
                }
            }

            return obj;
        }

        public static object GetPropertyValue(object propertyValue, Type fieldType)
        {
            if (fieldType.FullName == "System.Object")
            {
                return propertyValue;
            }

            var enumerableInterface = fieldType.Name == "IEnumerable`1"
              ? fieldType
              : fieldType.GetInterface("IEnumerable`1");

            if (fieldType.Name != "String"
                && enumerableInterface != null)
            {
                var elementType = enumerableInterface.GetGenericArguments()[0];
                var underlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;
                var genericListType = typeof(List<>).MakeGenericType(elementType);
                var newArray = (IList) Activator.CreateInstance(genericListType);

                var valueList = propertyValue as IEnumerable;
                if (valueList == null) return newArray;

                foreach (var listItem in valueList)
                {
                    newArray.Add(listItem == null ? null : GetPropertyValue(listItem, underlyingType));
                }

                return newArray;
            }

            var value = propertyValue;

            var nullableFieldType = Nullable.GetUnderlyingType(fieldType);

            // if this is a nullable type and the value is null, return null
            if(nullableFieldType != null && value == null)
            {
                return null;
            }

            if(nullableFieldType != null)
            {
                fieldType = nullableFieldType;
            }

            if (propertyValue is Dictionary<string, object>)
            {
                return ToObject(propertyValue as Dictionary<string, object>, fieldType);
            }

            if (fieldType.GetTypeInfo().IsEnum)
            {
                if (value == null)
                {
                    var enumNames = Enum.GetNames(fieldType);
                    value = enumNames[0];
                }

                if (!IsDefinedEnumValue(fieldType, value))
                {
                    throw new ExecutionError($"Unknown value '{value}' for enum '{fieldType.Name}'.");
                }

                var str = value.ToString();
                value = Enum.Parse(fieldType, str, true);
            }

            return ConvertValue(value, fieldType);
        }

        public static object GetProperyValue(this object obj, string propertyName)
        {
            var val = obj.GetType()
                .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                .GetValue(obj, null);

            return val;
        }


        public static Type GetInterface(this Type type, string name)
        {
            return type.GetInterfaces().FirstOrDefault(x => x.Name == name);
        }

        public static object ConvertValue(object value, Type fieldType)
        {
            if (value == null) return null;

            if (fieldType == typeof(DateTime) && value is DateTime)
            {
                return value;
            }

            var text = value.ToString();
            return _conversions.Value.Convert(fieldType, text);
        }

        public static T GetPropertyValue<T>(this object value)
        {
            return (T)GetPropertyValue(value, typeof(T));
        }

        public static bool IsDefinedEnumValue(Type type, object value)
        {
            var names = Enum.GetNames(type);
            if (names.Contains(value?.ToString() ?? "", StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            var underlyingType = Enum.GetUnderlyingType(type);
            var converted = Convert.ChangeType(value, underlyingType);

            var values = Enum.GetValues(type);

            foreach (var val in values)
            {
                var convertedVal = Convert.ChangeType(val, underlyingType);
                if (convertedVal.Equals(converted))
                {
                    return true;
                }
            }

            return false;
        }

        public static IDictionary<string, object> AsDictionary(
            this object source,
            BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            return source
                .GetType()
                .GetProperties(flags)
                .ToDictionary
                (
                    propInfo => propInfo.Name,
                    propInfo => propInfo.GetValue(source, null)
                );
        }
    }
}

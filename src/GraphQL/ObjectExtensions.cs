using GraphQL.Conversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace GraphQL
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Creates a new instance of the indicated type, populating it with the dictionary.
        /// </summary>
        /// <typeparam name="T">The type to create.</typeparam>
        /// <param name="source">The source of values.</param>
        /// <returns>T.</returns>
        public static T ToObject<T>(this IDictionary<string, object> source)
            where T : class, new()
        {
            return (T)ToObject(source, typeof(T));
        }

        /// <summary>
        /// Creates a new instance of the indicated type, populating it with the dictionary.
        /// </summary>
        /// <param name="source">The source of values.</param>
        /// <param name="type">The type to create.</param>
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

        /// <summary>
        /// Converts the indicated value into a type that is compatible with fieldType.
        /// </summary>
        /// <param name="propertyValue">The value to be converted.</param>
        /// <param name="fieldType">The desired type.</param>
        /// <remarks>There is special handling for strings, IEnumerable&lt;T&gt;, Nullable&lt;T&gt;, and Enum.</remarks>
        public static object GetPropertyValue(this object propertyValue, Type fieldType)
        {
            // Short-circuit conversion if the property value already
            if (fieldType.IsInstanceOfType(propertyValue))
            {
                return propertyValue;
            }

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
                IList newArray;
                var elementType = enumerableInterface.GetGenericArguments()[0];
                var underlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;
                var implementsIList = fieldType.GetInterface("IList") != null;

                if (implementsIList && !fieldType.IsArray)
                {
                    newArray = (IList)Activator.CreateInstance(fieldType);
                }
                else
                {
                    var genericListType = typeof(List<>).MakeGenericType(elementType);
                    newArray = (IList)Activator.CreateInstance(genericListType);
                }

                var valueList = propertyValue as IEnumerable;
                if (valueList == null) return newArray;

                foreach (var listItem in valueList)
                {
                    newArray.Add(listItem == null ? null : GetPropertyValue(listItem, underlyingType));
                }

                if (fieldType.IsArray)
                {
                    var array = Array.CreateInstance(elementType, newArray.Count);
                    newArray.CopyTo(array, 0);
                    return array;
                }

                return newArray;
            }

            var value = propertyValue;

            var nullableFieldType = Nullable.GetUnderlyingType(fieldType);

            // if this is a nullable type and the value is null, return null
            if (nullableFieldType != null && value == null)
            {
                return null;
            }

            if (nullableFieldType != null)
            {
                fieldType = nullableFieldType;
            }

            if (propertyValue is Dictionary<string, object> objects)
            {
                return ToObject(objects, fieldType);
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

        private static object ConvertValue(object value, Type targetType)
        {
            return ValueConverter.ConvertTo(value, targetType);
        }

        /// <summary>
        /// Gets the value of the named property.
        /// </summary>
        /// <param name="obj">The object to be read.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>System.Object.</returns>
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            var val = obj.GetType()
                .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(obj, null);

            return val;
        }


        /// <summary>
        /// Returns an interface implemented by the indicated type whose name matches the desired name.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="name">The name of the desired interface. This is case sensitive.</param>
        /// <returns>The interface, or <c>null</c> if no matches were found.</returns>
        /// <remarks>If more than one interface matches, the returned interface is non-deterministic.</remarks>
        public static Type GetInterface(this Type type, string name)
        {
            return type.GetInterfaces().FirstOrDefault(x => x.Name == name);
        }

        public static T GetPropertyValue<T>(this object value)
        {
            return (T)GetPropertyValue(value, typeof(T));
        }

        /// <summary>
        /// Returns true is the value is null, value.ToString equals an empty string, or the value can be converted into a named enum value.
        /// </summary>
        /// <param name="type">An enum type.</param>
        /// <param name="value">The value being tested.</param>
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

        /// <summary>
        /// Converts an object into a dictionary.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="flags">The binding flags used to control which properties are read.</param>
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

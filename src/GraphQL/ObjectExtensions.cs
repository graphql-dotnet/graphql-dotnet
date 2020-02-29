using GraphQL.Types;
using System;
using System.Collections;
using System.Collections.Generic;
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
            where T : class
            => (T)ToObject(source, typeof(T));

        /// <summary>
        /// Creates a new instance of the indicated type, populating it with the dictionary.
        /// Can use any constructor of the indicated type, provided that there are keys in the
        /// dictionary that correspond (case sensitive) to the names of the constructor parameters.
        /// </summary>
        /// <param name="source">The source of values.</param>
        /// <param name="type">The type to create.</param>
        /// <param name="mappedType">
        /// GraphType for matching dictionary keys with <paramref name="type"/> property names.
        /// GraphType contains information about this matching in Metadata property.
        /// In case of configuring field as Field(x => x.FName).Name("FirstName") source dictionary
        /// will have 'FirstName' key but its value should be set to 'FName' property of created object.   
        /// </param>
        public static object ToObject(this IDictionary<string, object> source, Type type, IGraphType mappedType = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // force sourceType to be IDictionary<string, object>
            if (ValueConverter.TryConvertTo(source, type, out object result, typeof(IDictionary<string, object>)))
                return result;

            // attempt to use the most specific constructor sorting in decreasing order of number of parameters
            var ctorCandidates = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderByDescending(ctor => ctor.GetParameters().Length);

            ConstructorInfo targetCtor = null;
            ParameterInfo[] ctorParameters = null;

            foreach (var ctor in ctorCandidates)
            {
                var parameters = ctor.GetParameters();
                if (parameters.All(p => source.ContainsKey(p.Name)))
                {
                    targetCtor = ctor;
                    ctorParameters = parameters;
                    break;
                }
            }

            if (targetCtor == null)
                throw new ArgumentException($"Type '{type}' does not contain a constructor that could be used for current input arguments.", nameof(type));

            object[] ctorArguments = ctorParameters.Length == 0 ? Array.Empty<object>() : new object[ctorParameters.Length];

            for (int i = 0; i < ctorParameters.Length; ++i)
            {
                var arg = GetPropertyValue(source[ctorParameters[i].Name], ctorParameters[i].ParameterType);
                ctorArguments[i] = arg;
            }

            var obj = targetCtor.Invoke(ctorArguments);

            var complexType = mappedType.GetNamedType() as IComplexGraphType;

            foreach (var item in source)
            {
                // these parameters have already been used in the constructor
                if (ctorParameters.Any(p => p.Name == item.Key))
                    continue;

                // type may not contain mapping information
                var field = complexType?.GetField(item.Key);
                string propertyName = field?.GetMetadata(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME, item.Key) ?? item.Key;

                var propertyInfo = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    var value = GetPropertyValue(item.Value, propertyInfo.PropertyType, field?.ResolvedType);
                    propertyInfo.SetValue(obj, value, null); //issue: this works even if propertyInfo is ValueType and value is null
                }
            }

            return obj;
        }

        /// <summary>
        /// Converts the indicated value into a type that is compatible with fieldType.
        /// </summary>
        /// <param name="propertyValue">The value to be converted.</param>
        /// <param name="fieldType">The desired type.</param>
        /// <param name="mappedType">
        /// GraphType for matching dictionary keys with <paramref name="type"/> property names.
        /// GraphType contains information about this matching in Metadata property.
        /// In case of configuring field as Field(x => x.FName).Name("FirstName") source dictionary
        /// will have 'FirstName' key but its value should be set to 'FName' property of created object.   
        /// </param>
        /// <remarks>There is special handling for strings, IEnumerable&lt;T&gt;, Nullable&lt;T&gt;, and Enum.</remarks>
        public static object GetPropertyValue(this object propertyValue, Type fieldType, IGraphType mappedType = null)
        {
            // Short-circuit conversion if the property value already of the right type
            if (propertyValue == null || fieldType == typeof(object) || fieldType.IsInstanceOfType(propertyValue))
            {
                return propertyValue;
            }

            if (ValueConverter.TryConvertTo(propertyValue, fieldType, out object result))
                return result;

            var enumerableInterface = fieldType.Name == "IEnumerable`1"
              ? fieldType
              : fieldType.GetInterface("IEnumerable`1");

            if (fieldType != typeof(string) && enumerableInterface != null)
            {
                IList newCollection;
                var elementType = enumerableInterface.GetGenericArguments()[0];
                var underlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;
                var fieldTypeImplementsIList = fieldType.GetInterface("IList") != null;

                var propertyValueAsIList = propertyValue as IList;

                // Custom container
                if (fieldTypeImplementsIList && !fieldType.IsArray)
                {
                    newCollection = (IList)Activator.CreateInstance(fieldType);
                }
                // Array of known size is created immediately
                else if (fieldType.IsArray && propertyValueAsIList != null)
                {
                    newCollection = Array.CreateInstance(elementType, propertyValueAsIList.Count);
                }
                // List<T>
                else
                {
                    var genericListType = typeof(List<>).MakeGenericType(elementType);
                    newCollection = (IList)Activator.CreateInstance(genericListType);
                }

                if (!(propertyValue is IEnumerable valueList))
                    return newCollection;

                // Array of known size is populated in-place
                if (fieldType.IsArray && propertyValueAsIList != null)
                {
                    for (int i = 0; i < propertyValueAsIList.Count; ++i)
                    {
                        var listItem = propertyValueAsIList[i];
                        newCollection[i] = listItem == null ? null : GetPropertyValue(listItem, underlyingType, mappedType);
                    }
                }
                // Array of unknown size is created only after populating list
                else
                {
                    foreach (var listItem in valueList)
                    {
                        newCollection.Add(listItem == null ? null : GetPropertyValue(listItem, underlyingType, mappedType));
                    }
                   
                    if (fieldType.IsArray)
                        newCollection = ((dynamic)newCollection).ToArray();
                }

                return newCollection;
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

            if (propertyValue is IDictionary<string, object> objects)
            {
                return ToObject(objects, fieldType, mappedType);
            }

            if (fieldType.IsEnum)
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

                string str = value.ToString();
                value = Enum.Parse(fieldType, str, true);
            }

            return ValueConverter.ConvertTo(value, fieldType);
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

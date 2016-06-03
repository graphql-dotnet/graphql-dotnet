using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL
{
    public static class ObjectExtensions
    {
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
                    var fieldType = propertyType.PropertyType;
                    if (fieldType.Name != "String"
                             && fieldType.GetInterface("IEnumerable`1") != null)
                    {
                        var elementType = fieldType.GetGenericArguments()[0];
                        var genericListType = typeof(List<>).MakeGenericType(elementType);
                        var newArray = (IList)Activator.CreateInstance(genericListType);

                        var valueList = item.Value as IEnumerable;

                        if (valueList != null)
                        {
                            foreach (var listItem in valueList)
                            {
                                var value = listItem;
                                newArray.Add(Convert.ChangeType(value, elementType));
                            }
                        }

                        propertyType.SetValue(obj, newArray, null);
                    }
                    else
                    {
                        var value = item.Value;

                        var isNullable = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>);
                        if (isNullable)
                        {
                            fieldType = fieldType.GenericTypeArguments.First();
                        }

                        propertyType.SetValue(obj, isNullable ? value : Convert.ChangeType(value, fieldType), null);
                    }
                }
            }

            return obj;
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

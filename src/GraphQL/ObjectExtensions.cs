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
            var obj = new T();
            var type = obj.GetType();

            foreach (var item in source)
            {
                var propertyType =
                    type.GetProperty(item.Key,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyType != null)
                {
                    if (propertyType.PropertyType.Name != "String"
                        && propertyType.PropertyType.GetInterface("IEnumerable`1") != null)
                    {
                        var elementType = propertyType.PropertyType.GetGenericArguments()[0];
                        var genericListType = typeof(List<>).MakeGenericType(elementType);
                        var newArray = (IList)Activator.CreateInstance(genericListType);

                        var valueList = item.Value as IEnumerable;

                        if (valueList != null)
                        {
                            foreach (var listItem in valueList)
                            {
                                newArray.Add(Convert.ChangeType(listItem, elementType));
                            }
                        }

                        propertyType.SetValue(
                            obj,
                            newArray,
                            null);
                    }
                    else
                    {
                        propertyType.SetValue(
                            obj,
                            Convert.ChangeType(item.Value, propertyType.PropertyType),
                            null);
                    }
                }
            }

            return obj;
        }

        public static IDictionary<string, object> AsDictionary(
            this object source,
            BindingFlags flags
                = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
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

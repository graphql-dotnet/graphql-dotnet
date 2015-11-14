using System;
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
                    propertyType.SetValue(
                        obj,
                        Convert.ChangeType(item.Value, propertyType.PropertyType),
                        null);
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

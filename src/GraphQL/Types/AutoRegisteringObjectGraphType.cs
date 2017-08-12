using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL.Types
{
    public class AutoRegisteringObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        public AutoRegisteringObjectGraphType()
        {
            var properties = typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.PropertyType.GetTypeInfo().IsValueType || p.PropertyType == typeof(string));

            foreach (var propertyInfo in properties)
            {
                Field(propertyInfo.PropertyType.GetGraphTypeFromType(propertyInfo.PropertyType.IsNullable()), propertyInfo.Name);
            }
        }
    }
}
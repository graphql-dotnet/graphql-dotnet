using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL.Types
{
    public class AutoRegisteringObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        public AutoRegisteringObjectGraphType()
        {
            var properties = GetPropertiesToRegister();

            foreach (var propertyInfo in properties)
            {
                Field(propertyInfo.PropertyType.GetGraphTypeFromType(propertyInfo.PropertyType.IsNullable()), propertyInfo.Name);
            }
        }

        protected virtual IEnumerable<PropertyInfo> GetPropertiesToRegister()
        {
            return typeof(TSourceType)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.GetTypeInfo().IsValueType || p.PropertyType == typeof(string));
        }
    }
}
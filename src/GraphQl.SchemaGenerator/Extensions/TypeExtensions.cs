using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GraphQL.SchemaGenerator.Attributes;

namespace GraphQL.SchemaGenerator.Extensions
{
    //todo: allow easier customizations of this fuctionality
    public static class TypeExtensions
    {
        public static bool ShouldIncludeInGraph(this Type type)
        {
            var types = type.GetCustomAttributes(typeof(GraphTypeAttribute), false);
            var dataContracts = type.GetCustomAttributes(typeof(DataContractAttribute), false);

            return types.Any() || dataContracts.Any();
        }

        public static bool ShouldIncludeMemberInGraph(this FieldInfo field)
        {
           return ShouldIncludeMemberInGraph(field.GetCustomAttributes(false));
        }

        public static bool ShouldIncludeMemberInGraph(this PropertyInfo property)
        {
            return ShouldIncludeMemberInGraph(property.GetCustomAttributes(false));
        }

        public static bool ShouldIncludeMemberInGraph(object[] attributes)
        {
            if (Enumerable.Any(attributes.Where(t => t is GraphTypeAttribute), i=> ((GraphTypeAttribute)i).Exclude))
            {
                return false;
            }

            var exclude = Enumerable.Any(attributes, a => a.GetType().Name.StartsWith("JsonIgnore", StringComparison.OrdinalIgnoreCase));

            return !exclude;
        }
    }
}

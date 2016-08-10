using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GraphQl.SchemaGenerator.Attributes;

namespace GraphQl.SchemaGenerator.Extensions
{
    //todo: allow easier customizations
    internal static class TypeExtensions
    {
        public static bool ShouldIncludeInGraph(this Type type)
        {
            var types = type.GetCustomAttributes(typeof(GraphTypeAttribute), false);

            return types.Any();
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
            if (attributes.Where(t => t is GraphTypeAttribute).Any(i=> ((GraphTypeAttribute)i).Exclude))
            {
                return false;
            }

            var exclude = attributes.Any(a => a.GetType().Name.StartsWith("JsonIgnore", StringComparison.OrdinalIgnoreCase));

            return !exclude;
        }
    }
}

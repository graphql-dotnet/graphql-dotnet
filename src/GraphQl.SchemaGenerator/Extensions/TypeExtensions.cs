using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace GraphQl.SchemaGenerator.Extensions
{
    internal static class TypeExtensions
    {
        public static bool ShouldIncludeInGraph(this Type type)
        {
            return type.IsDefined(typeof(DataContractAttribute));
        }

        public static bool ShouldIncludeMemberInGraph(this FieldInfo field)
        {
            return field.IsDefined(typeof(DataMemberAttribute));
        }

        public static bool ShouldIncludeMemberInGraph(this PropertyInfo property)
        {
            return property.IsDefined(typeof(DataMemberAttribute));
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    internal static class AutoRegisteringHelper
    {
        internal static IEnumerable<PropertyInfo> ExcludeProperties<TSourceType>(IEnumerable<PropertyInfo> properties, params Expression<Func<TSourceType, object?>>[]? excludedProperties)
            => excludedProperties == null || excludedProperties.Length == 0
                ? properties
                : properties.Where(propertyInfo => !excludedProperties!.Any(p => GetPropertyName(p) == propertyInfo.Name));

        internal static void SetFields<TSourceType>(ComplexGraphType<TSourceType> type, IEnumerable<PropertyInfo> properties)
        {
            type.Name = typeof(TSourceType).GraphQLName();

            foreach (var propertyInfo in properties)
            {
                type.Field(
                    type: propertyInfo.PropertyType.GetGraphTypeFromType(IsNullableProperty(propertyInfo), type is IInputObjectGraphType ? TypeMappingMode.InputType : TypeMappingMode.OutputType),
                    name: propertyInfo.Name,
                    description: propertyInfo.Description(),
                    deprecationReason: propertyInfo.ObsoleteMessage()
                ).DefaultValue = (propertyInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute)?.Value;
            }
        }

        private static bool IsNullableProperty(PropertyInfo propertyInfo)
        {
            if (Attribute.IsDefined(propertyInfo, typeof(RequiredAttribute)))
                return false;

            if (!propertyInfo.PropertyType.IsValueType)
                return true;

            return propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static string GetPropertyName<TSourceType>(Expression<Func<TSourceType, object?>> expression)
        {
            if (expression.Body is MemberExpression m1)
                return m1.Member.Name;

            if (expression.Body is UnaryExpression u && u.Operand is MemberExpression m2)
                return m2.Member.Name;

            throw new NotSupportedException($"Unsupported type of expression: {expression.GetType().Name}");
        }
    }
}

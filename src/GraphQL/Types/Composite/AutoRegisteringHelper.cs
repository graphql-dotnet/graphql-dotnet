using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    internal static class AutoRegisteringHelper
    {
        /// <summary>
        /// Scans a specific CLR type for <see cref="GraphQLAttribute"/> attributes and applies
        /// them to to the specified <see cref="IGraphType"/>.
        /// </summary>
        internal static void ApplyGraphQLAttributes<TSourceType>(IGraphType graphType)
        {
            // Description and deprecation reason are already set in ComplexGraphType<TSourceType> constructor

            // Apply derivatives of GraphQLAttribute
            var attributes = typeof(TSourceType).GetCustomAttributes<GraphQLAttribute>();
            foreach (var attr in attributes)
            {
                attr.Modify(graphType);
            }
        }

        /// <summary>
        /// Filters an enumeration of <see cref="PropertyInfo"/> values to exclude specified properties.
        /// </summary>
        internal static IEnumerable<PropertyInfo> ExcludeProperties<TSourceType>(IEnumerable<PropertyInfo> properties, params Expression<Func<TSourceType, object?>>[]? excludedProperties)
            => excludedProperties == null || excludedProperties.Length == 0
                ? properties
                : properties.Where(propertyInfo => !excludedProperties!.Any(p => GetPropertyName(p) == propertyInfo.Name));

        internal static FieldType CreateField(PropertyInfo propertyInfo, bool isInputType)
        {
            var fieldType = new FieldType()
            {
                Name = propertyInfo.Name,
                Description = propertyInfo.Description(),
                DeprecationReason = propertyInfo.ObsoleteMessage(),
                Type = propertyInfo.PropertyType.GetGraphTypeFromType(IsNullableProperty(propertyInfo), isInputType ? TypeMappingMode.InputType : TypeMappingMode.OutputType),
                DefaultValue = isInputType ? propertyInfo.DefaultValue() : null,
            };

            // Apply derivatives of GraphQLAttribute
            var attributes = propertyInfo.GetCustomAttributes<GraphQLAttribute>();
            foreach (var attr in attributes)
            {
                attr.Modify(fieldType, isInputType);
            }

            return fieldType;
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

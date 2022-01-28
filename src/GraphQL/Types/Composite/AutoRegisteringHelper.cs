using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Resolvers;

namespace GraphQL.Types
{
    internal static class AutoRegisteringHelper
    {
        /// <summary>
        /// Scans a specific CLR type for <see cref="GraphQLAttribute"/> attributes and applies
        /// them to the specified <see cref="IGraphType"/>.
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

        /// <summary>
        /// Creates a <see cref="FieldType"/> for the specified <see cref="MemberInfo"/>.
        /// </summary>
        internal static FieldType CreateField(MemberInfo memberInfo, Type graphType, bool isInputType)
        {
            var fieldType = new FieldType()
            {
                Name = memberInfo.Name,
                Description = memberInfo.Description(),
                DeprecationReason = memberInfo.ObsoleteMessage(),
                Type = graphType,
                DefaultValue = isInputType ? memberInfo.DefaultValue() : null,
                Resolver = isInputType ? null : MemberResolver.Create(memberInfo),
            };
            if (isInputType)
            {
                fieldType.WithMetadata(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME, memberInfo.Name);
            }

            // Apply derivatives of GraphQLAttribute
            var attributes = memberInfo.GetCustomAttributes<GraphQLAttribute>();
            foreach (var attr in attributes)
            {
                attr.Modify(fieldType, isInputType);
            }

            return fieldType;
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

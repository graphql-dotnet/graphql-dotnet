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
        internal static void ConfigureGraphType<TSourceType>(IGraphType graphType)
        {
            var classType = typeof(TSourceType);

            // Apply [Description] attribute
            var descriptionAttribute = classType.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
                graphType.Description = descriptionAttribute.Description;

            // Apply [Obsolete] attribute
            var obsoleteAttribute = classType.GetCustomAttribute<ObsoleteAttribute>();
            if (obsoleteAttribute != null)
                graphType.DeprecationReason = obsoleteAttribute.Message;

            // Apply derivatives of GraphQLAttribute
            var attributes = classType.GetCustomAttributes<GraphQLAttribute>();
            foreach (var attr in attributes)
            {
                attr.Modify(graphType);
            }
        }

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
                DefaultValue = (propertyInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute)?.Value,
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

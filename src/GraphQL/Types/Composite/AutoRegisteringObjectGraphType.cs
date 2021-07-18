using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types
{
    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the XML comments.
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    public class AutoRegisteringObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringObjectGraphType() : this(null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludedProperties"> Expressions for excluding fields, for example 'o => o.Age'. </param>
        public AutoRegisteringObjectGraphType(params Expression<Func<TSourceType, object>>[]? excludedProperties)
        {
            AutoRegisteringHelper.SetFields(this, GetRegisteredProperties(), excludedProperties);
        }

        /// <summary>
        /// Returns a list of properties that should have fields created for them.
        /// </summary>
        protected virtual IEnumerable<PropertyInfo> GetRegisteredProperties() => typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified input type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the XML comments.
    /// Note that now __InputValue has no isDeprecated and deprecationReason fields but in the future they may appear - https://github.com/graphql/graphql-spec/pull/525
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    public class AutoRegisteringInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
    {
        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringInputObjectGraphType() : this(null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludedProperties"> Expressions for excluding fields, for example 'o => o.Age'. </param>
        public AutoRegisteringInputObjectGraphType(params Expression<Func<TSourceType, object>>[]? excludedProperties)
        {
            AutoRegisteringHelper.SetFields(this, GetRegisteredProperties(), excludedProperties);
        }

        /// <summary>
        /// Returns a list of properties that should have fields created for them.
        /// </summary>
        protected virtual IEnumerable<PropertyInfo> GetRegisteredProperties() => typeof(TSourceType).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    internal static class AutoRegisteringHelper
    {
        internal static void SetFields<TSourceType>(ComplexGraphType<TSourceType> type, IEnumerable<PropertyInfo> properties, params Expression<Func<TSourceType, object>>[]? excludedProperties)
        {
            type.Name = typeof(TSourceType).GraphQLName();

            foreach (var propertyInfo in properties)
            {
                if (excludedProperties?.Any(p => GetPropertyName(p) == propertyInfo.Name) == true)
                    continue;

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

        private static string GetPropertyName<TSourceType>(Expression<Func<TSourceType, object>> expression)
        {
            if (expression.Body is MemberExpression m1)
                return m1.Member.Name;

            if (expression.Body is UnaryExpression u && u.Operand is MemberExpression m2)
                return m2.Member.Name;

            throw new NotSupportedException($"Unsupported type of expression: {expression.GetType().Name}");
        }
    }
}

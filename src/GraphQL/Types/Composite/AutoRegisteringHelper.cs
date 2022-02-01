using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
            };
            if (isInputType)
            {
                fieldType.WithMetadata(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME, memberInfo.Name);
            }

            return fieldType;
        }

        /// <summary>
        /// Applies <see cref="GraphQLAttribute"/>s defined on <paramref name="memberInfo"/> to <paramref name="fieldType"/>.
        /// </summary>
        internal static void ApplyFieldAttributes(MemberInfo memberInfo, FieldType fieldType, bool isInputType)
        {
            // Apply derivatives of GraphQLAttribute
            var attributes = memberInfo.GetCustomAttributes<GraphQLAttribute>();
            foreach (var attr in attributes)
            {
                attr.Modify(fieldType, isInputType);
            }
        }

        private static string GetPropertyName<TSourceType>(Expression<Func<TSourceType, object?>> expression)
        {
            if (expression.Body is MemberExpression m1)
                return m1.Member.Name;

            if (expression.Body is UnaryExpression u && u.Operand is MemberExpression m2)
                return m2.Member.Name;

            throw new NotSupportedException($"Unsupported type of expression: {expression.GetType().Name}");
        }

        /// <summary>
        /// Constructs a lambda expression for a field resolver to return the specified query argument
        /// from the resolve context.
        /// </summary>
        internal static LambdaExpression GetParameterExpression(Type parameterType, QueryArgument queryArgument)
        {
            //construct a call to ResolveFieldContextExtensions.GetArgument, passing in the appropriate default value
            var getArgumentMethodTyped = _getArgumentMethod.MakeGenericMethod(parameterType);
            var resolveFieldContextParameter = Expression.Parameter(typeof(IResolveFieldContext), "context");
            var queryArgumentExpression = Expression.Constant(queryArgument, typeof(QueryArgument));
            //e.g. Func<IResolveFieldContext, int> = (context) => ResolveFieldContextExtensions.GetArgument<int>(context, queryArgument.Name, queryArgument.DefaultValue);
            var expr = Expression.Call(getArgumentMethodTyped, resolveFieldContextParameter, queryArgumentExpression);

            return Expression.Lambda(
                expr,
                resolveFieldContextParameter);
        }

        private static readonly MethodInfo _getArgumentMethod = typeof(AutoRegisteringHelper).GetMethod(nameof(GetArgumentInternal), BindingFlags.NonPublic | BindingFlags.Static)!;
        /// <summary>
        /// Returns the value for the specified query argument, or the default value of the query argument
        /// if a value was not specified in the request.
        /// <br/><br/>
        /// Unlike <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)"/>,
        /// the default value is not returned if <see langword="null"/> was supplied.
        /// </summary>
        private static T? GetArgumentInternal<T>(IResolveFieldContext context, QueryArgument queryArgument)
        {
            // GetArgument changes null values to DefaultValue even if null is explicitly specified,
            // so do not use GetArgument here
            return context.TryGetArgument(typeof(T), queryArgument.Name, out var value)
                ? (T?)value
                : (T?)queryArgument.DefaultValue;
        }

        private static readonly PropertyInfo _sourceProperty = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.Source), BindingFlags.Instance | BindingFlags.Public);
        /// <summary>
        /// Returns an expression that gets the source and casts it to the specified type.
        /// </summary>
        internal static LambdaExpression GetSourceExpression(Type sourceType)
        {
            //retrieve the value and cast it to the specified type
            //e.g. Func<IResolveFieldContext, TSourceType> = (context) => (TSourceType)context.Source;
            var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
            return Expression.Lambda(
                Expression.Convert(Expression.Property(param, _sourceProperty), sourceType),
                param);
        }
    }
}

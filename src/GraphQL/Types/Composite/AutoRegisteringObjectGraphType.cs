using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the xml comments.
    /// </summary>
    /// <typeparam name="TSourceType"></typeparam>
    public class AutoRegisteringObjectGraphType<TSourceType> : ObjectGraphType<TSourceType>
    {
        ///// <summary>
        ///// Register all methods of <see cref="GetRegisteredMethods"/>
        ///// </summary>
        //public bool RegisterMethods { get; set; }
        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/>.
        /// </summary>
        public AutoRegisteringObjectGraphType() : this(false, null) { }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="excludesMembers"> Expressions for excluding fields, for example 'o => o.Age' or 'o => o.Age(defaul, ...). </param>
        public AutoRegisteringObjectGraphType(params Expression<Func<TSourceType, object>>[] excludesMembers): this(false, excludesMembers)
        {
        }

        /// <summary>
        /// Creates a GraphQL type from <typeparamref name="TSourceType"/> by specifying fields to exclude from registration.
        /// </summary>
        /// <param name="registerMethods"> Register methods of <see cref="GetRegisteredMethods"/> </param>
        /// <param name="excludesMembers"> Expressions for excluding fields, for example 'o => o.Age' or 'o => o.Age(defaul, ...). </param>
        public AutoRegisteringObjectGraphType(bool registerMethods, params Expression<Func<TSourceType, object>>[] excludesMembers)
        {
            Name = typeof(TSourceType).GraphQLName();
            AutoRegisteringHelper.SetFields(this, GetRegisteredProperties(), excludesMembers);
            if (registerMethods)
                AutoRegisteringHelper.SetFields(this, GetRegisteredMethods(), excludesMembers);
        }

        protected virtual IEnumerable<PropertyInfo> GetRegisteredProperties()
        {
            return typeof(TSourceType)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => AutoRegisteringHelper.IsEnabledForRegister(p.PropertyType, true));
        }

        protected virtual IEnumerable<MethodInfo> GetRegisteredMethods()
        {
            return typeof(TSourceType)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && AutoRegisteringHelper.IsEnabledForRegister(m.ReturnType, true));
        }
    }

    /// <summary>
    /// Allows you to automatically register the necessary fields for the specified input type.
    /// Supports <see cref="DescriptionAttribute"/>, <see cref="ObsoleteAttribute"/>, <see cref="DefaultValueAttribute"/> and <see cref="RequiredAttribute"/>.
    /// Also it can get descriptions for fields from the xml comments.
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
        public AutoRegisteringInputObjectGraphType(params Expression<Func<TSourceType, object>>[] excludedProperties)
        {
            Name = typeof(TSourceType).GraphQLName();
            AutoRegisteringHelper.SetFields(this, GetRegisteredProperties(), excludedProperties);
        }

        protected virtual IEnumerable<PropertyInfo> GetRegisteredProperties()
        {
            return typeof(TSourceType)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => AutoRegisteringHelper.IsEnabledForRegister(p.PropertyType, true));
        }
    }

    internal static class AutoRegisteringHelper
    {
        internal static void SetFields<TSourceType>(ComplexGraphType<TSourceType> type, IEnumerable<PropertyInfo> properties, params Expression<Func<TSourceType, object>>[] excludedProperties)
        {
            foreach (var propertyInfo in properties)
            {
                if (excludedProperties?.Any(p => GetMemberInfo(p) == propertyInfo) == true)
                    continue;

                type.Field(
                    type: propertyInfo.PropertyType.GetGraphTypeFromType(propertyInfo.GraphTypeIsNullable()),
                    name: propertyInfo.Name,
                    description: propertyInfo.Description(),
                    deprecationReason: propertyInfo.ObsoleteMessage()
                ).DefaultValue = (propertyInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute)?.Value;
            }
        }

        internal static void SetFields<TSourceType>(ComplexGraphType<TSourceType> type, IEnumerable<MethodInfo> methods, params Expression<Func<TSourceType, object>>[] excludedMethods)
        {
            foreach (var methodInfo in methods)
            {
                if (excludedMethods?.Any(p => GetMemberInfo(p) == methodInfo) == true)
                    continue;

                var methodResolver = new MethodResolver(methodInfo);

                methodResolver.CreateQueryArguments();

                var result = type.Field(
                    type: methodInfo.ReturnType.GetGraphTypeFromType(methodInfo.GraphTypeIsNullable()),
                    name: methodInfo.Name,
                    arguments: methodResolver.CreateQueryArguments(),
                    description: methodInfo.Description(),
                    deprecationReason: methodInfo.ObsoleteMessage()
                );

                result.Resolver = methodResolver;
            }
        }


        private static MemberInfo GetMemberInfo(LambdaExpression expression)
        {
            var expr = (expression.Body is UnaryExpression u) ? u.Operand : expression.Body;

            if (expr is MemberExpression m)
                return m.Member;

            if (expr is MethodCallExpression c)
                return c.Method;

            throw new NotSupportedException($"Unsupported type of expression: {expr.GetType().Name}");
        }

        internal static bool IsEnabledForRegister(Type returnType, bool firstCall)
        {
            if (returnType == typeof(string))
                return true;

            if (returnType.IsValueType)
                return true; // TODO: requires discussion: Nullable<T>, enums, any struct

            if (GraphTypeTypeRegistry.Contains(returnType))
                return true;

            if (firstCall)
            {
                var realType = GetRealType(returnType);
                if (realType != returnType)
                    return IsEnabledForRegister(realType, false);
            }

            return false;
        }

        private static Type GetRealType(Type returnType)
        {

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return returnType.GetGenericArguments()[0];
            }

            if (returnType.IsArray)
            {
                return returnType.GetElementType();
            }

            if (returnType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(returnType))
            {
                return returnType.GetEnumerableElementType();
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return returnType.GetGenericArguments()[0];
            }

            return returnType;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A field resolver for a specific <see cref="MemberInfo"/>.
    /// Returns the value of the field or property, or for methods, calls the method and returns the value of the method.
    /// </summary>
    internal class MemberResolver : IFieldResolver
    {
        private readonly Func<object?, object?> _resolver;

        private MemberResolver(MemberInfo memberInfo)
        {
            var parameter = Expression.Parameter(typeof(object), "x");
            Expression expression = memberInfo switch
            {
                MethodInfo methodInfo => Expression.Call(
                    methodInfo.IsStatic
                        ? null
                        : Expression.Convert(parameter, methodInfo.DeclaringType!),
                    methodInfo),
                PropertyInfo propertyInfo => Expression.MakeMemberAccess(
                    (propertyInfo.GetMethod ?? throw new ArgumentOutOfRangeException(nameof(memberInfo), "Property does not have get method.")).IsStatic
                        ? null
                        : Expression.Convert(parameter, propertyInfo.DeclaringType!),
                    propertyInfo),
                FieldInfo fieldInfo => Expression.MakeMemberAccess(
                    fieldInfo.IsStatic
                        ? null
                        : Expression.Convert(parameter, fieldInfo.DeclaringType!),
                    fieldInfo),
                _ => throw new ArgumentOutOfRangeException(nameof(memberInfo), "Only properties, methods and fields are supported."),
            };
            var convertToObjectExpression = Expression.Convert(expression, typeof(object));
            var lambda = Expression.Lambda<Func<object?, object?>>(convertToObjectExpression, parameter);
            _resolver = lambda.Compile();
        }

        private static readonly ConcurrentDictionary<MemberInfo, MemberResolver> _resolverDictionary = new();
        private static readonly Func<MemberInfo, MemberResolver> _createFunc
            = (MemberInfo memberInfo) => new MemberResolver(memberInfo);
        public static MemberResolver Create(MemberInfo memberInfo)
            => _resolverDictionary.GetOrAdd(memberInfo, _createFunc);

        public object? Resolve(IResolveFieldContext context) => _resolver(context.Source);
    }
}

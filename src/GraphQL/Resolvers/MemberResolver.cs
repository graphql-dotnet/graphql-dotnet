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
        private readonly Func<object?, ValueTask<object?>> _resolver;

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
                    (propertyInfo.GetMethod ?? throw new ArgumentOutOfRangeException(nameof(memberInfo), $"Property '{propertyInfo.Name}' does not have get method.")).IsStatic
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
            Expression valueTaskExpression;
            if (expression.Type == typeof(ValueTask<object>))
            {
                valueTaskExpression = expression;
            }
            else if (expression.Type == typeof(Task<object>))
            {
                valueTaskExpression = Expression.New(_valueTaskTaskConstructor, expression);
            }
            else if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                valueTaskExpression = Expression.Call(
                    null,
                    _convertTaskMethod.MakeGenericMethod(expression.Type.GetGenericArguments()[0]),
                    expression);
            }
            else if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                valueTaskExpression = Expression.Call(
                    null,
                    _convertValueTaskMethod.MakeGenericMethod(expression.Type.GetGenericArguments()[0]),
                    expression);
            }
            else
            {
                valueTaskExpression = Expression.New(
                    _valueTaskObjectConstructor,
                    Expression.Convert(expression, typeof(object)));
            }
            var lambda = Expression.Lambda<Func<object?, ValueTask<object?>>>(valueTaskExpression, parameter);
            _resolver = lambda.Compile();
        }

        private static readonly MethodInfo _convertTaskMethod = typeof(MemberResolver).GetMethod(nameof(ConvertTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo _convertValueTaskMethod = typeof(MemberResolver).GetMethod(nameof(ConvertValueTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly ConstructorInfo _valueTaskTaskConstructor = typeof(ValueTask<object?>).GetConstructor(new Type[] { typeof(Task<object?>) })!;
        private static readonly ConstructorInfo _valueTaskObjectConstructor = typeof(ValueTask<object?>).GetConstructor(new Type[] { typeof(object) })!;

        private static async ValueTask<object?> ConvertTaskAsync<T>(Task<T> task)
            => await task.ConfigureAwait(false);

        private static async ValueTask<object?> ConvertValueTaskAsync<T>(ValueTask<T> task)
            => await task.ConfigureAwait(false);

        private static readonly ConcurrentDictionary<MemberInfo, MemberResolver> _resolverDictionary = new();
        public static MemberResolver Create(MemberInfo memberInfo)
            => _resolverDictionary.GetOrAdd(memberInfo, info => new MemberResolver(info));

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context) => _resolver(context.Source);
    }
}

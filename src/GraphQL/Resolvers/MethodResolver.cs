using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A field resolver for a specific <see cref="MethodInfo"/>.
    /// Calls the specified method (with the specified arguments) and returns the value of the method.
    /// </summary>
    internal class MemberResolver : IFieldResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<object?>> _resolver;

        public MemberResolver(FieldInfo fieldInfo, LambdaExpression sourceExpression)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException(nameof(fieldInfo));
            if (sourceExpression == null)
                throw new ArgumentNullException(nameof(sourceExpression));
            if (sourceExpression.Parameters.Count != 1 ||
                sourceExpression.Parameters[0].Type != typeof(IResolveFieldContext) ||
                !fieldInfo.DeclaringType!.IsAssignableFrom(sourceExpression.ReturnType))
            {
                throw new ArgumentException($"Source lambda must be of type Func<IResolveFieldContext, {fieldInfo.DeclaringType!.Name}>.", nameof(sourceExpression));
            }
            var methodCallExpr = Expression.MakeMemberAccess(
                fieldInfo.IsStatic ? null : sourceExpression.Body,
                fieldInfo);

            _resolver = BuildFunction(sourceExpression.Parameters[0], methodCallExpr);
        }

        public MemberResolver(PropertyInfo propertyInfo, LambdaExpression sourceExpression)
            : this(propertyInfo.GetMethod ?? throw new ArgumentException("No 'get' method for the supplied property.", nameof(propertyInfo)), sourceExpression, Array.Empty<LambdaExpression>())
        {
        }

        public MemberResolver(MethodInfo methodInfo, LambdaExpression instanceExpression, IList<LambdaExpression> methodArgumentExpressions)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            if (instanceExpression == null)
                throw new ArgumentNullException(nameof(instanceExpression));
            if (methodArgumentExpressions == null)
                throw new ArgumentNullException(nameof(methodArgumentExpressions));

            // verify that the expressions provided match the number of parameters
            var methodParameters = methodInfo.GetParameters();
            if (methodArgumentExpressions.Count != methodParameters.Length)
            {
                throw new InvalidOperationException("The number of expressions must equal the number of method parameters.");
            }
            if (instanceExpression.Parameters.Count != 1 ||
                instanceExpression.Parameters[0].Type != typeof(IResolveFieldContext) ||
                !methodInfo.DeclaringType!.IsAssignableFrom(instanceExpression.ReturnType))
            {
                throw new ArgumentException($"Source lambda must be of type Func<IResolveFieldContext, {methodInfo.DeclaringType!.Name}>.", nameof(instanceExpression));
            }

            // create a parameter expression for IResolveFieldContext
            var resolveFieldContextParameter = Expression.Parameter(typeof(IResolveFieldContext), "context");

            // convert each of the provided lambda expressions and pull the expression bodies, replacing the existing lambda's
            // parameter with the new one, so each expression is using the same parameter
            var expressionBodies = new Expression[methodArgumentExpressions.Count];
            for (int i = 0; i < methodArgumentExpressions.Count; i++)
            {
                var expr = methodArgumentExpressions[i];
                if (expr.Parameters.Count != 1 || expr.Parameters[0].Type != typeof(IResolveFieldContext) || expr.ReturnType != methodParameters[i].ParameterType)
                {
                    throw new InvalidOperationException($"A supplied expression is not a lambda delegate of type Func<IResolveFieldContext, {methodParameters[i].ParameterType.Name}>.");
                }
                expressionBodies[i] = expr.Body.Replace(expr.Parameters[0], resolveFieldContextParameter);
            }

            // create the method call expression
            var methodCallExpr =
                Expression.Call(
                    methodInfo.IsStatic
                        ? null
                        : instanceExpression.Body.Replace(
                            instanceExpression.Parameters[0],
                            resolveFieldContextParameter),
                    methodInfo,
                    expressionBodies);

            _resolver = BuildFunction(resolveFieldContextParameter, methodCallExpr);
        }

        internal static Func<IResolveFieldContext, ValueTask<object?>> BuildFunction<TSourceType, TProperty>(Expression<Func<TSourceType, TProperty>> lambdaExpression)
        {
            Expression<Func<IResolveFieldContext, TSourceType>> sourceExpression = context => (TSourceType)context.Source!;
            var body = lambdaExpression.Body.Replace(lambdaExpression.Parameters[0], sourceExpression.Body);
            return BuildFunction(sourceExpression.Parameters[0], body);
        }

        /// <summary>
        /// Creates an appropriate function based on the return type of the expression body.
        /// </summary>
        private static Func<IResolveFieldContext, ValueTask<object?>> BuildFunction(ParameterExpression resolveFieldContextParameter, Expression bodyExpression)
        {
            Expression? valueTaskExpr = null;

            if (bodyExpression.Type == typeof(ValueTask<object?>))
            {
                valueTaskExpr = bodyExpression;
            }
            else if (bodyExpression.Type.IsGenericType)
            {
                var genericType = bodyExpression.Type.GetGenericTypeDefinition();
                if (genericType == typeof(ValueTask<>))
                {
                    var underlyingType = bodyExpression.Type.GetGenericArguments()[0];
                    var method = _marshalValueTaskAsyncMethod.MakeGenericMethod(underlyingType);
                    valueTaskExpr = Expression.Call(
                        method,
                        bodyExpression);
                }
                else if (genericType == typeof(Task<>))
                {
                    var underlyingType = bodyExpression.Type.GetGenericArguments()[0];
                    var method = _marshalTaskAsyncMethod.MakeGenericMethod(underlyingType);
                    valueTaskExpr = Expression.Call(
                        method,
                        bodyExpression);
                }
            }

            if (valueTaskExpr == null)
            {
                // convert the result to type object
                Expression convertExpr = bodyExpression.Type == typeof(object)
                    ? bodyExpression
                    : Expression.Convert(bodyExpression, typeof(object));

                var valueTaskType = typeof(ValueTask<object?>);
                var constructor = valueTaskType.GetConstructor(new Type[] { typeof(object) });
                valueTaskExpr = Expression.New(constructor, convertExpr);
            }

            // create the lambda
            var lambdaExpr = Expression.Lambda<Func<IResolveFieldContext, ValueTask<object?>>>(
                valueTaskExpr,
                resolveFieldContextParameter);

            // compile the lambda expression
            return lambdaExpr.Compile();
        }

        private static readonly MethodInfo _marshalTaskAsyncMethod = typeof(MemberResolver).GetMethod(nameof(MarshalTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<object?> MarshalTaskAsync<T>(Task<T> task) => await task.ConfigureAwait(false);

        private static readonly MethodInfo _marshalValueTaskAsyncMethod = typeof(MemberResolver).GetMethod(nameof(MarshalValueTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<object?> MarshalValueTaskAsync<T>(ValueTask<T> task) => await task.ConfigureAwait(false);

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context) => _resolver(context);
    }
}

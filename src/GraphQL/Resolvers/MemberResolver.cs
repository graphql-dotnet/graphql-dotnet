using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A precompiled field resolver for a specific <see cref="MethodInfo"/>, <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>.
    /// Returns the specified field or property, or for methods, calls the specified method (with the specified arguments)
    /// and returns the value of the method.
    /// </summary>
    public class MemberResolver : IFieldResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<object?>> _resolver;

        /// <summary>
        /// Initializes an instance for the specified field, using the specified instance expression to access the instance of the field.
        /// <br/><br/>
        /// An example of an instance expression would be as follows:
        /// <code>context =&gt; (TSourceType)context.Source</code>
        /// </summary>
        public MemberResolver(FieldInfo fieldInfo, LambdaExpression instanceExpression)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException(nameof(fieldInfo));
            if (instanceExpression == null)
                throw new ArgumentNullException(nameof(instanceExpression));

            if (instanceExpression.Parameters.Count != 1 ||
                instanceExpression.Parameters[0].Type != typeof(IResolveFieldContext) ||
                !fieldInfo.DeclaringType!.IsAssignableFrom(instanceExpression.ReturnType))
            {
                throw new ArgumentException($"Source lambda must be of type Func<IResolveFieldContext, {fieldInfo.DeclaringType!.Name}>.", nameof(instanceExpression));
            }

            var methodCallExpr = Expression.MakeMemberAccess(
                fieldInfo.IsStatic ? null : instanceExpression.Body,
                fieldInfo);

            _resolver = BuildFieldResolver(instanceExpression.Parameters[0], methodCallExpr);
        }

        /// <summary>
        /// Initializes an instance for the specified property, using the specified instance expression to access the instance of the property.
        /// <br/><br/>
        /// An example of an instance expression would be as follows:
        /// <code>context =&gt; (TSourceType)context.Source</code>
        /// </summary>
        public MemberResolver(PropertyInfo propertyInfo, LambdaExpression instanceExpression)
            : this((propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo))).GetMethod ?? throw new ArgumentException($"No 'get' method for the supplied {propertyInfo.Name} property.", nameof(propertyInfo)), instanceExpression, Array.Empty<LambdaExpression>())
        {
        }

        /// <summary>
        /// Initializes an instance for the specified method, using the specified instance expression to access the instance of the method,
        /// along with a list of arguments to be passed to the method. The method argument expressions must have return types that match
        /// those of the method arguments.
        /// <br/><br/>
        /// An example of an instance expression would be as follows:
        /// <code>context =&gt; (TSourceType)context.Source</code>
        /// </summary>
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

            _resolver = BuildFieldResolver(resolveFieldContextParameter, methodCallExpr);
        }

        /// <summary>
        /// Creates an appropriate resolver function based on the return type of the expression body.
        /// </summary>
        protected virtual Func<IResolveFieldContext, ValueTask<object?>> BuildFieldResolver(ParameterExpression resolveFieldContextParameter, Expression bodyExpression)
            => BuildFieldResolverInternal(resolveFieldContextParameter, bodyExpression);

        /// <inheritdoc cref="BuildFieldResolver(ParameterExpression, Expression)"/>
        internal static Func<IResolveFieldContext, ValueTask<object?>> BuildFieldResolverInternal(ParameterExpression resolveFieldContextParameter, Expression bodyExpression)
        {
            Expression? valueTaskExpr = null;

            if (bodyExpression.Type == typeof(ValueTask<object?>))
            {
                valueTaskExpr = bodyExpression;
            }
            else if (bodyExpression.Type == typeof(Task<object?>))
            {
                // e.g. valueTask = new ValueTask<object>(body);
                var valueTaskType = typeof(ValueTask<object?>);
                var constructor = valueTaskType.GetConstructor(new Type[] { typeof(Task<object?>) })!;
                valueTaskExpr = Expression.New(constructor, bodyExpression);
            }
            else if (bodyExpression.Type.IsGenericType)
            {
                var genericType = bodyExpression.Type.GetGenericTypeDefinition();
                if (genericType == typeof(ValueTask<>))
                {
                    // e.g. valueTask = MarshalValueTask(body);
                    var underlyingType = bodyExpression.Type.GetGenericArguments()[0];
                    var method = _marshalValueTaskAsyncMethod.MakeGenericMethod(underlyingType);
                    valueTaskExpr = Expression.Call(
                        method,
                        bodyExpression);
                }
                else if (genericType == typeof(Task<>))
                {
                    // e.g. valueTask = MarshalTask(body);
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
                // e.g. var convert = (object)body;
                Expression convertExpr = bodyExpression.Type == typeof(object)
                    ? bodyExpression
                    : Expression.Convert(bodyExpression, typeof(object));

                // e.g. valueTask = new ValueTask<object>(convert);
                var valueTaskType = typeof(ValueTask<object?>);
                var constructor = valueTaskType.GetConstructor(new Type[] { typeof(object) })!;
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

        /// <inheritdoc/>
        public virtual ValueTask<object?> ResolveAsync(IResolveFieldContext context) => _resolver(context);
    }
}

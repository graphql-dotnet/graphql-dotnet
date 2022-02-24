using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A precompiled field resolver for a specific <see cref="MethodInfo"/>, <see cref="PropertyInfo"/> or <see cref="FieldInfo"/>.
    /// Returns the specified field or property, or for methods, calls the specified method (with the specified arguments)
    /// and returns the value of the method.
    /// </summary>
    internal class MemberResolver : IFieldResolver
    {
        private readonly Func<IResolveFieldContext, object?> _resolver;

        /// <summary>
        /// Initializes an instance for the specified field, using a default source expression of:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// </summary>
        public MemberResolver(FieldInfo fieldInfo)
            : this(fieldInfo, null)
        {
        }

        /// <summary>
        /// Initializes an instance for the specified field, using the specified source expression to access the instance of the field.
        /// If <paramref name="sourceExpression"/> is <see langword="null"/> then a default source expression is used as follows:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// </summary>
        public MemberResolver(FieldInfo fieldInfo, LambdaExpression? sourceExpression)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException(nameof(fieldInfo));
            sourceExpression ??= BuildDefaultSourceExpression(fieldInfo.DeclaringType);

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

        /// <summary>
        /// Initializes an instance for the specified property, using a default source expression of:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// </summary>
        public MemberResolver(PropertyInfo propertyInfo)
            : this(propertyInfo, null)
        {
        }

        /// <summary>
        /// Initializes an instance for the specified property, using the specified source expression to access the instance of the property.
        /// If <paramref name="sourceExpression"/> is <see langword="null"/> then a default source expression is used as follows:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// </summary>
        public MemberResolver(PropertyInfo propertyInfo, LambdaExpression? sourceExpression)
            : this((propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo))).GetMethod ?? throw new ArgumentException("No 'get' method for the supplied property.", nameof(propertyInfo)), Array.Empty<LambdaExpression>(), sourceExpression)
        {
        }

        /// <summary>
        /// Initializes an instance for the specified method and arguments, using a default source expression of:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// The method argument expressions must have return types that match those of the method arguments.
        /// </summary>
        public MemberResolver(MethodInfo methodInfo, IList<LambdaExpression> methodArgumentExpressions)
            : this(methodInfo, methodArgumentExpressions, null)
        {
        }

        /// <summary>
        /// Initializes an instance for the specified method, using the specified source expression to access the instance of the method,
        /// along with a list of arguments to be passed to the method. The method argument expressions must have return types that match
        /// those of the method arguments.
        /// If <paramref name="sourceExpression"/> is <see langword="null"/> then a default source expression is used as follows:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// </summary>
        public MemberResolver(MethodInfo methodInfo, IList<LambdaExpression> methodArgumentExpressions, LambdaExpression? sourceExpression)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            sourceExpression ??= BuildDefaultSourceExpression(methodInfo.DeclaringType);
            if (methodArgumentExpressions == null)
                throw new ArgumentNullException(nameof(methodArgumentExpressions));
            // verify that the expressions provided match the number of parameters
            var methodParameters = methodInfo.GetParameters();
            if (methodArgumentExpressions.Count != methodParameters.Length)
            {
                throw new InvalidOperationException("The number of expressions must equal the number of method parameters.");
            }
            if (sourceExpression.Parameters.Count != 1 ||
                sourceExpression.Parameters[0].Type != typeof(IResolveFieldContext) ||
                !methodInfo.DeclaringType!.IsAssignableFrom(sourceExpression.ReturnType))
            {
                throw new ArgumentException($"Source lambda must be of type Func<IResolveFieldContext, {methodInfo.DeclaringType!.Name}>.", nameof(sourceExpression));
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
                        : sourceExpression.Body.Replace(
                            sourceExpression.Parameters[0],
                            resolveFieldContextParameter),
                    methodInfo,
                    expressionBodies);

            _resolver = BuildFunction(resolveFieldContextParameter, methodCallExpr);
        }

        /// <summary>
        /// Creates an appropriate resolver function based on the return type of the expression body.
        /// </summary>
        private static Func<IResolveFieldContext, object?> BuildFunction(ParameterExpression resolveFieldContextParameter, Expression bodyExpression)
        {
            // convert the result to type object
            var convertExpr = Expression.Convert(bodyExpression, typeof(object));

            // create the lambda
            var lambdaExpr = Expression.Lambda<Func<IResolveFieldContext, object>>(
                convertExpr,
                resolveFieldContextParameter);

            // compile the lambda expression
            return lambdaExpr.Compile();
        }

        private static readonly PropertyInfo _sourcePropertyInfo = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.Source))!;
        private static LambdaExpression BuildDefaultSourceExpression(Type sourceType)
        {
            var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
            var body = Expression.MakeMemberAccess(param, _sourcePropertyInfo);
            var castExpr = Expression.Convert(body, sourceType);
            return Expression.Lambda(castExpr, param);
        }

        /// <inheritdoc/>
        public object? Resolve(IResolveFieldContext context) => _resolver(context);
    }
}

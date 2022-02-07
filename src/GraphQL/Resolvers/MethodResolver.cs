using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A field resolver for a specific <see cref="MethodInfo"/>.
    /// Calls the specified method (with the specified arguments) and returns the value of the method.
    /// </summary>
    internal class MethodResolver : IFieldResolver
    {
        private readonly Func<IResolveFieldContext, object?> _resolver;

        private static readonly PropertyInfo _resolveFieldContextSourceParameter = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.Source))!;
        public MethodResolver(MethodInfo methodInfo, List<LambdaExpression> methodArgumentExpressions)
        {
            // verify that the expressions provided match the number of parameters
            var methodParameters = methodInfo.GetParameters();
            if (methodArgumentExpressions.Count != methodParameters.Length)
            {
                throw new InvalidOperationException("The number of expressions must equal the number of method parameters.");
            }

            // create a parameter expression for IResolveFieldContext
            var resolveFieldContextParameter = Expression.Parameter(typeof(IResolveFieldContext), "context");

            // convert each of the provided lambda expressions and pull the expression bodies, replacing the existing lambda's
            // parameter with the new one, so each expression is using the same parameter
            var expressionBodies = new Expression[methodArgumentExpressions.Count];
            for (int i = 0; i < methodArgumentExpressions.Count; i++)
            {
                var expr = methodArgumentExpressions[i];
                if (expr.Parameters.Count != 1 || expr.Parameters[0].Type != typeof(IResolveFieldContext))
                {
                    throw new InvalidOperationException("A supplied expression is not a lambda delegate of type Func<IResolveFieldContext, T>.");
                }
                var replaced = expr.Body.Replace(expr.Parameters[0], resolveFieldContextParameter);
                // reduce unnessessary convertion by unwrapping expression so there is no double conversion one line later
                if (replaced is UnaryExpression unary && unary.NodeType == ExpressionType.Convert && unary.Operand.Type == methodParameters[i].ParameterType)
                    replaced = unary.Operand;
                expressionBodies[i] = replaced.Type == methodParameters[i].ParameterType ? replaced : Expression.Convert(replaced, methodParameters[i].ParameterType);
            }

            // create the method call expression
            var methodCallExpr =
                Expression.Call(
                    methodInfo.IsStatic
                        ? null
                        : Expression.Convert(
                            Expression.MakeMemberAccess(
                                resolveFieldContextParameter,
                                _resolveFieldContextSourceParameter),
                            methodInfo.DeclaringType!),
                    methodInfo,
                    expressionBodies);

            // convert the result to type object
            var convertExpr = Expression.Convert(methodCallExpr, typeof(object));

            // create the lambda
            var lambdaExpr = Expression.Lambda<Func<IResolveFieldContext, object>>(
                convertExpr,
                resolveFieldContextParameter);

            // compile the lambda expression
            _resolver = lambdaExpr.Compile();
        }

        public object? Resolve(IResolveFieldContext context) => _resolver(context);
    }
}

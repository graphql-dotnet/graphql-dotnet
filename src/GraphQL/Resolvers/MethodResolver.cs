using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A field resolver for a specific <see cref="MemberInfo"/>.
    /// Returns the value of the field or property, or for methods, calls the method and returns the value of the method.
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

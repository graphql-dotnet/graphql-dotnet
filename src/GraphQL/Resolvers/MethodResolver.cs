using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// Experimental. Probably should be a part of <see cref="NameFieldResolver"/>
    /// It has <see cref="CreateQueryArguments"/> for field registration
    /// </summary>
    internal class MethodResolver : IFieldResolver
    {
        public static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> cache = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();
        private readonly MethodInfo _methodInfo;

        public MethodResolver(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            var parameters = _methodInfo.GetParameters();

            var args = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.ParameterType == typeof(IResolveFieldContext))
                {
                    args[i] = context;
                }
                else
                {
                    args[i] = context.GetArgument(p.ParameterType, p.Name);
                }
            }

            var resolveFunc = CreateOrGetResolveFunc(_methodInfo);
            return resolveFunc(context.Source, args);
        }

        private static Func<object, object[], object> CreateOrGetResolveFunc(MethodInfo methodInfo)
            => cache.GetOrAdd(methodInfo, m =>
            {

                /* 
                 * body is like:
                 * object resolveFunc(object source, object[] params) => (object)((SourceType)source).method((int)params[0], (srting)params[1], ... );
                */

                var sourceExprParam = Expression.Parameter(typeof(object));
                var argsExprParams = Expression.Parameter(typeof(object[]));

                var callExpr = Expression.Call(
                    Expression.Convert(sourceExprParam, m.DeclaringType),
                    m,
                    m.GetParameters().Select((_, i) => Expression.Convert(Expression.ArrayIndex(argsExprParams, Expression.Constant(i)), _.ParameterType))
                );

                return (Func<object, object[], object>)Expression.Lambda(Expression.Convert(callExpr, typeof(object)), sourceExprParam, argsExprParams).Compile();
            });

        public QueryArguments CreateQueryArguments()
        {
            var parameters = _methodInfo.GetParameters();

            var parametersForArgs = parameters.Where(_ => _.ParameterType != typeof(IResolveFieldContext)).ToArray();
            if (parametersForArgs.Length == 0)
                return null;


            var arguments = new QueryArguments();
            foreach (var parameterInfo in parametersForArgs)
            {
                arguments.Add(new QueryArgument(parameterInfo.ParameterType.GetGraphTypeFromType(parameterInfo.GraphTypeIsNullable())) { Name = parameterInfo.Name });
            }

            return arguments;
        }
    }
}

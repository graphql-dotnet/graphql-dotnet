using GraphQL.Types;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    public static class FieldResolverBuilderExtensions
    {
        private const string InvokeMethodName = "Resolve";

        public static IFieldMiddlewareBuilder Use<T>(this IFieldMiddlewareBuilder builder) where T : new() => Use(builder, typeof(T));

        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, System.Type middleware)
        {
            var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var invokeMethods = methods.Where(m => string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal)).ToArray();
            if (invokeMethods.Length > 1)
            {
                throw new InvalidOperationException($"There should be only a single method named {InvokeMethodName}. Middleware actually has {invokeMethods.Length} methods.");
            }

            if (invokeMethods.Length == 0)
            {
                throw new InvalidOperationException($"Could not find a method named {InvokeMethodName}. Middleware must have a public instance method named {InvokeMethodName}.");
            }

            var methodInfo = invokeMethods[0];
            if (!typeof(Task<object>).IsAssignableFrom(methodInfo.ReturnType))
            {
                throw new InvalidOperationException($"The {InvokeMethodName} method should return a Task<object>.");
            }

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(ResolveFieldContext) || parameters[1].ParameterType != typeof(FieldMiddlewareDelegate))
            {
                throw new InvalidOperationException($"The {InvokeMethodName} method of middleware should take a parameter of type {nameof(ResolveFieldContext)} as the first parameter and a parameter of type {nameof(FieldMiddlewareDelegate)} as the second parameter.");
            }

            //func = (context, next) => (new <middleware>()).Resolve(context, next);
            var paramContext = Expression.Parameter(typeof(ResolveFieldContext));
            var paramNext = Expression.Parameter(typeof(FieldMiddlewareDelegate));
            var func = Expression.Lambda<Func<ResolveFieldContext, FieldMiddlewareDelegate, Task<object>>>(
                Expression.Call(
                    Expression.New(middleware),
                    methodInfo,
                    paramContext,
                    paramNext),
                paramContext,
                paramNext)
                .Compile();

            return builder.Use(next =>
            {
                return context => func(context, next);
            });
        }
    }
}

using GraphQL.Types;
using System;
using System.Linq;
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
            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(IResolveFieldContext) || parameters[1].ParameterType != typeof(FieldMiddlewareDelegate))
            {
                throw new InvalidOperationException($"The {InvokeMethodName} method of middleware should take a parameter of type {nameof(IResolveFieldContext)} as the first parameter and a parameter of type {nameof(FieldMiddlewareDelegate)} as the second parameter.");
            }

            return builder.Use(next =>
            {
                var instance = Activator.CreateInstance(middleware);

                return context => (Task<object>)methodInfo.Invoke(instance, new object[] { context, next });
            });
        }
    }
}

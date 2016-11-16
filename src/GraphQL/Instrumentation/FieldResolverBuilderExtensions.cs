using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public static class FieldResolverBuilderExtensions
    {
        public static string InvokeMethodName = "Resolve";

        public static IFieldMiddlewareBuilder Use<T>(this IFieldMiddlewareBuilder builder)
        {
            return Use(builder, typeof(T));
        }

        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, System.Type middleware)
        {
            return builder.Use(next =>
            {
                var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var invokeMethods = methods.Where(m => string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal)).ToArray();
                if (invokeMethods.Length > 1)
                {
                    throw new InvalidOperationException($"There should be only a single method named {InvokeMethodName}.");
                }

                if (invokeMethods.Length == 0)
                {
                    throw new InvalidOperationException($"Could not find a method named {InvokeMethodName}");
                }

                var methodinfo = invokeMethods[0];
                if (!typeof(Task<object>).IsAssignableFrom(methodinfo.ReturnType))
                {
                    throw new InvalidOperationException($"The {InvokeMethodName} method should return a Task<object>.");
                }

                var parameters = methodinfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(ResolveFieldContext))
                {
                    throw new InvalidOperationException($"The {InvokeMethodName} method should take a parameter of type ResolveFieldContext as the first parameter.");
                }

                if (parameters.Length == 1 || parameters[1].ParameterType != typeof(FieldMiddlewareDelegate))
                {
                    throw new InvalidOperationException($"The {InvokeMethodName} method should take a parameter of type {typeof(FieldMiddlewareDelegate).Name} as the second parameter.");
                }

                var instance = Activator.CreateInstance(middleware);

                return context => (Task<object>)methodinfo.Invoke(instance, new object[] { context, next });
            });
        }
    }
}

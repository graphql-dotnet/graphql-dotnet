using GraphQL.Types;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    public static class FieldResolverBuilderExtensions
    {
        private const string INVOKE_METHOD_NAME = "Resolve";

        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, IFieldMiddleware middleware)
            => builder.Use(next => context => middleware.Resolve(context, next));

        public static IFieldMiddlewareBuilder Use<T>(this IFieldMiddlewareBuilder builder) => Use(builder, typeof(T));

        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, System.Type middleware)
        {
            // if IFieldMiddleware interface is supported, then just call its Resolve method
            if (typeof(IFieldMiddleware).IsAssignableFrom(middleware))
            {
                return builder.Use(next =>
                {
                    return context =>
                    {
                        return context.Schema is Schema schema
                            ? CheckNotNull((IFieldMiddleware)schema.Services.GetService(middleware), middleware).Resolve(context, next)
                            : throw new NotSupportedException();
                    };
                });
            }
            // otherwise, we first find this method on the type and then call it through reflection
            else
            {
                var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var invokeMethods = methods.Where(m => string.Equals(m.Name, INVOKE_METHOD_NAME, StringComparison.Ordinal)).ToArray();
                if (invokeMethods.Length > 1)
                {
                    throw new InvalidOperationException($"There should be only a single method named {INVOKE_METHOD_NAME}. Middleware actually has {invokeMethods.Length} methods.");
                }

                if (invokeMethods.Length == 0)
                {
                    throw new InvalidOperationException($"Could not find a method named {INVOKE_METHOD_NAME}. Middleware must have a public instance method named {INVOKE_METHOD_NAME}.");
                }

                var methodInfo = invokeMethods[0];
                if (!typeof(Task<object>).IsAssignableFrom(methodInfo.ReturnType))
                {
                    throw new InvalidOperationException($"The {INVOKE_METHOD_NAME} method should return a Task<object>.");
                }

                var parameters = methodInfo.GetParameters();
                if (parameters.Length != 2 || parameters[0].ParameterType != typeof(IResolveFieldContext) || parameters[1].ParameterType != typeof(FieldMiddlewareDelegate))
                {
                    throw new InvalidOperationException($"The {INVOKE_METHOD_NAME} method of middleware should take a parameter of type {nameof(IResolveFieldContext)} as the first parameter and a parameter of type {nameof(FieldMiddlewareDelegate)} as the second parameter.");
                }

                return builder.Use(next =>
                {
                    return context =>
                    {
                        return context.Schema is Schema schema
                            ? (Task<object>)methodInfo.Invoke(CheckNotNull(schema.Services.GetService(middleware), middleware), new object[] { context, next })
                            : throw new NotSupportedException();
                    };
                });
            }
        }

        private static T CheckNotNull<T>(T instance, System.Type type)
        {
            if (instance == null)
                throw new InvalidOperationException($"Field middleware of type '{type.FullName}' must be registered in schema.Services.");
            return instance;
        }
    }
}

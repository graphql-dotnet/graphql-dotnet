using System;
using System.Linq;
using System.Reflection;

namespace GraphQL.Reflection
{
    internal static class ReflectionHelper
    {
        /// <summary>
        /// Creates an Accessor for the indicated GraphQL field
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="field">The desired field.</param>
        /// <param name="resolverType">defaults to Resolver</param>
        public static IAccessor ToAccessor(this Type type, string field, ResolverType resolverType)
        {
            if (type == null)
                return null;

            var methodInfo = type.MethodForField(field, resolverType);
            if (methodInfo != null)
            {
                return new SingleMethodAccessor(methodInfo);
            }

            if (resolverType != ResolverType.Resolver)
            {
                return null;
            }

            var propertyInfo = type.PropertyForField(field);
            if (propertyInfo != null)
            {
                return new SinglePropertyAccessor(propertyInfo);
            }

            return null;
        }

        /// <summary>
        /// Returns the method associated with the indicated GraphQL field
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="field">The desired field.</param>
        /// <param name="resolverType">Indicates if a resolver or subscriber method is requested.</param>
        public static MethodInfo MethodForField(this Type type, string field, ResolverType resolverType)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var method = methods.FirstOrDefault(m =>
            {
                var attr = m.GetCustomAttribute<GraphQLMetadataAttribute>();
                var name = attr?.Name ?? m.Name;
                return string.Equals(field, name, StringComparison.OrdinalIgnoreCase) && resolverType == (attr?.Type ?? ResolverType.Resolver);
            });

            return method;
        }

        /// <summary>
        /// Returns the property associated with the indicated GraphQL field
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="field">The desired field.</param>
        public static PropertyInfo PropertyForField(this Type type, string field)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var property = properties.FirstOrDefault(m =>
            {
                var attr = m.GetCustomAttribute<GraphQLMetadataAttribute>();
                var name = attr?.Name ?? m.Name;
                return string.Equals(field, name, StringComparison.OrdinalIgnoreCase);
            });

            return property;
        }

        public static object[] BuildArguments(ParameterInfo[] parameters, IResolveFieldContext context)
        {
            if (parameters == null || parameters.Length == 0)
                return null;

            object[] arguments = new object[parameters.Length];

            var index = 0;
            if (parameters[index].ParameterType.IsAssignableFrom(context.GetType()))
            {
                arguments[index] = context;
                index++;
            }

            if (parameters.Length > index
                && context.Source != null
                && (context.Source?.GetType() == parameters[index].ParameterType
                    || string.Equals(parameters[index].Name, "source", StringComparison.OrdinalIgnoreCase)))
            {
                arguments[index] = context.Source;
                index++;
            }

            if (parameters.Length > index
                && context.UserContext != null
                && parameters[index].ParameterType.IsInstanceOfType(context.UserContext))
            {
                arguments[index] = context.UserContext;
                index++;
            }

            foreach (var parameter in parameters.Skip(index))
            {
                arguments[index] = context.GetArgument(parameter.ParameterType, parameter.Name);
                index++;
            }

            return arguments;
        }
    }
}

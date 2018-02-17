using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Reflection
{
    internal static class ReflectionHelper
    {

        /// <summary>
        /// Creates an Accessor for the indicated GraphQL field
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="field">The desired field.</param>
        public static Accessor ToAccessor(this Type type, string field)
        {
            if(type == null) return null;

            var methodInfo = type.MethodForField(field);
            if(methodInfo != null)
            {
                return new SingleMethodAccessor(methodInfo);
            }

            var propertyInfo = type.PropertyForField(field);
            if(propertyInfo != null)
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
        public static MethodInfo MethodForField(this Type type, string field)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var method = methods.FirstOrDefault(m =>
            {
                var attr = m.GetCustomAttribute<GraphQLMetadataAttribute>();
                var name = attr?.Name ?? m.Name;
                return string.Equals(field, name, StringComparison.OrdinalIgnoreCase);
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
    }

    /// <summary>
    /// An abstraction around accessing a property or method on a object instance
    /// </summary>
    public interface Accessor
    {
        string FieldName { get; }
        Type ReturnType { get; }
        Type DeclaringType { get; }
        ParameterInfo[] Parameters { get; }
        object GetValue(object target, object[] arguments);
        IEnumerable<T> GetAttributes<T>() where T : Attribute;
    }

    internal class SinglePropertyAccessor : Accessor
    {
        private PropertyInfo _getter;

        public SinglePropertyAccessor(PropertyInfo getter)
        {
            _getter = getter;
        }

        public string FieldName => _getter.Name;
        public Type ReturnType => _getter.PropertyType;
        public Type DeclaringType => _getter.DeclaringType;
        public ParameterInfo[] Parameters => _getter.GetMethod.GetParameters();
        public IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return _getter.GetCustomAttributes<T>();
        }

        public object GetValue(object target, object[] arguments)
        {
            return _getter.GetValue(target, null);
        }
    }

    internal class SingleMethodAccessor : Accessor
    {
        private MethodInfo _getter;

        public SingleMethodAccessor(MethodInfo getter)
        {
            _getter = getter;
        }

        public string FieldName => _getter.Name;
        public Type ReturnType => _getter.ReturnType;
        public Type DeclaringType => _getter.DeclaringType;
        public ParameterInfo[] Parameters => _getter.GetParameters();
        public IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return _getter.GetCustomAttributes<T>();
        }

        public object GetValue(object target, object[] arguments)
        {
            return _getter.Invoke(target, arguments);
        }
    }

    internal class AccessorResolver : FieldResolverBase
    {
        private readonly Accessor _accessor;
        private readonly IDependencyResolver _dependencyResolver;

        public AccessorResolver(Accessor accessor, IDependencyResolver dependencyResolver)
        {
            _accessor = accessor;
            _dependencyResolver = dependencyResolver;
        }

        public override object Resolve(ResolveFieldContext context)
        {
            var arguments = BuildArguments(_accessor.Parameters, context);

            var target = _dependencyResolver.Resolve(_accessor.DeclaringType);

            if (target == null)
            {
                var parentType = context.ParentType != null ? $"{context.ParentType.Name}." : null;
                throw new InvalidOperationException($"Could not resolve an instance of {_accessor.DeclaringType.Name} to execute {parentType}{context.FieldName}");
            }

            return _accessor.GetValue(target, arguments);
        }
    }

    internal abstract class FieldResolverBase : IFieldResolver
    {
        public abstract object Resolve(ResolveFieldContext context);

        protected object[] BuildArguments(ParameterInfo[] parameters, ResolveFieldContext context)
        {
            if(parameters == null || !parameters.Any()) return null;

            object[] arguments = new object[parameters.Length];

            var index = 0;
            if (typeof(ResolveFieldContext) == parameters[index].ParameterType)
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
                && context.UserContext?.GetType() == parameters[index].ParameterType)
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

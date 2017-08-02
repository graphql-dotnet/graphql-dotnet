using System;
using System.Linq;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class TypeConfig : MetadataProvider
    {
        public TypeConfig(string name)
        {
            Name = name;
        }

        public Type Type { get; set; }

        public string Name { get; }
        public string Description { get; set; }
        public Func<object, IObjectGraphType> ResolveType { get; set; }
        public Func<object, bool> IsTypeOfFunc { get; set; }

        public void IsTypeOf<T>()
        {
            IsTypeOfFunc = obj => obj?.GetType() == typeof(T);
        }

        public MethodInfo MethodForField(string field)
        {
            return Type?.MethodForField(field);
        }

        public IFieldResolver ResolverFor(string field, IDependencyResolver dependencyResolver)
        {
            if (Type == null)
            {
                return null;
            }

            var method = MethodForField(field);

            var resolverType = typeof(MethodModelBinderResolver<>).MakeGenericType(Type);

            var args = new object[] { method, dependencyResolver };
            var resolver = (IFieldResolver) Activator.CreateInstance(resolverType, args);

            return resolver;
        }
    }

    public interface IDependencyResolver
    {
        T Resolve<T>();
        object Resolve(Type type);
    }

    public class DefaultDependencyResolver : IDependencyResolver
    {
        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }

    internal class WrappedDependencyResolver : IDependencyResolver
    {
        public WrappedDependencyResolver(IDependencyResolver resolver)
        {
            InnerResolver = resolver;
        }

        public IDependencyResolver InnerResolver { get; set; }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            return InnerResolver.Resolve(type);
        }
    }

    public class MethodModelBinderResolver<T> : IFieldResolver
    {
        private readonly IDependencyResolver _dependencyResolver;
        private readonly MethodInfo _methodInfo;
        private readonly ParameterInfo[] _parameters;
        private readonly Type _type;

        public MethodModelBinderResolver(MethodInfo methodInfo, IDependencyResolver dependencyResolver)
        {
            _dependencyResolver = dependencyResolver;
            _type = typeof(T);
            _methodInfo = methodInfo;
            _parameters = _methodInfo.GetParameters();
        }

        public object Resolve(ResolveFieldContext context)
        {
            var index = 0;
            var arguments = new object[_parameters.Length];

            if (_parameters.Any() && context.Source?.GetType() == _parameters[0].ParameterType)
            {
                arguments[index] = context.Source;
                index++;
            }

            foreach (var parameter in _parameters.Skip(index))
            {
                arguments[index] = context.GetArgument(parameter.Name, parameter.ParameterType);
                index++;
            }

            var target = _dependencyResolver.Resolve(_type);

            if (target == null)
            {
                throw new InvalidOperationException($"Could not resolve an instance of {_type.Name} to execute {context.FieldName}");
            }

            return _methodInfo.Invoke(target, arguments);
        }
    }
}
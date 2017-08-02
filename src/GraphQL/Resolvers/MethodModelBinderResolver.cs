using System;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
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

            if (_parameters.Any() && typeof(ResolveFieldContext) == _parameters[index].ParameterType)
            {
                arguments[index] = context;
                index++;
            }

            if (_parameters.Any() && context.Source?.GetType() == _parameters[index].ParameterType)
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

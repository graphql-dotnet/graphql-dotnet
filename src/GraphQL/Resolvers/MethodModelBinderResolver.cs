using System;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class MethodModelBinderResolver<T> : IFieldResolver
    {
        private readonly Type _type;
        private readonly MethodInfo _methodInfo;
        private readonly IDependencyResolver _dependencyResolver;
        private readonly ParameterInfo[] _parameters;

        public MethodModelBinderResolver(MethodInfo methodInfo, IDependencyResolver dependencyResolver)
        {
            _type = typeof(T);
            _methodInfo = methodInfo;
            _dependencyResolver = dependencyResolver;
            _parameters = _methodInfo.GetParameters();
        }

        public bool RunThreaded()
        {
            return true;
        }

        public object Resolve(ResolveFieldContext context)
        {
            object[] arguments = null;

            if (_parameters.Any())
            {
                arguments = new object[_parameters.Length];

                var index = 0;
                if (typeof(ResolveFieldContext) == _parameters[index].ParameterType)
                {
                    arguments[index] = context;
                    index++;
                }

                if (_parameters.Length > index
                    && context.Source != null
                    && (context.Source?.GetType() == _parameters[index].ParameterType
                        || string.Equals(_parameters[index].Name, "source", StringComparison.OrdinalIgnoreCase)))
                {
                    arguments[index] = context.Source;
                    index++;
                }

                if (_parameters.Length > index
                    && context.UserContext != null
                    && context.UserContext?.GetType() == _parameters[index].ParameterType)
                {
                    arguments[index] = context.UserContext;
                    index++;
                }

                foreach (var parameter in _parameters.Skip(index))
                {
                    arguments[index] = context.GetArgument(parameter.ParameterType, parameter.Name);
                    index++;
                }
            }

            var target = _dependencyResolver.Resolve(_type);

            if (target == null)
            {
                var parentType = context.ParentType != null ? $"{context.ParentType.Name}." : null;
                throw new InvalidOperationException($"Could not resolve an instance of {_type.Name} to execute {parentType}{context.FieldName}");
            }

            return _methodInfo.Invoke(target, arguments);
        }
    }
}

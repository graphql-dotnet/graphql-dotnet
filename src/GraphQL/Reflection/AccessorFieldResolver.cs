using System;
using System.Linq;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Reflection
{
    internal class AccessorFieldResolver : IFieldResolver
    {
        private readonly IAccessor _accessor;
        private readonly IDependencyResolver _dependencyResolver;

        public AccessorFieldResolver(IAccessor accessor, IDependencyResolver dependencyResolver)
        {
            _accessor = accessor;
            _dependencyResolver = dependencyResolver;
        }

        public object Resolve(ResolveFieldContext context)
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

        private object[] BuildArguments(ParameterInfo[] parameters, ResolveFieldContext context)
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

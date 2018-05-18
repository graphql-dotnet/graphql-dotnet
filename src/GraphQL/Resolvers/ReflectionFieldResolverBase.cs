using System;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public abstract class ReflectionFieldResolverBase : IFieldResolver
    {
        public abstract object Resolve(ResolveFieldContext context);

        protected object[] BuildArguments(ParameterInfo[] parameters, ResolveFieldContext context)
        {
            if (parameters == null || !parameters.Any()) return null;

            var arguments = new object[parameters.Length];

            var index = 0;
            if (typeof(ResolveFieldContext) == parameters[index].ParameterType)
            {
                arguments[index] = context;
                index++;
            }

            if (parameters.Length > index
                && context.SourceObject != null
                && (context.SourceObject?.GetType() == parameters[index].ParameterType
                    || string.Equals(parameters[index].Name, "source", StringComparison.OrdinalIgnoreCase)))
            {
                arguments[index] = context.SourceObject;
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

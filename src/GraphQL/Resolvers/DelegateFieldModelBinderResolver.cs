using System;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class DelegateFieldModelBinderResolver : IFieldResolver
    {
        private readonly Delegate _resolver;
        private readonly ParameterInfo[] _parameters;

        public DelegateFieldModelBinderResolver(Delegate resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
            _parameters = _resolver.GetMethodInfo().GetParameters();
        }

        public object Resolve(ResolveFieldContext context)
        {
            object[] arguments = null;

            if (_parameters.Any())
            {
                var index = 0;
                arguments = new object[_parameters.Length];

                if (typeof(ResolveFieldContext) == _parameters[index].ParameterType)
                {
                    arguments[index] = context;
                    index++;
                }

                if (context.Source?.GetType() == _parameters[index].ParameterType)
                {
                    arguments[index] = context.Source;
                    index++;
                }

                foreach (var parameter in _parameters.Skip(index))
                {
                    arguments[index] = context.GetArgument(parameter.ParameterType, parameter.Name);
                    index++;
                }
            }

            return _resolver.DynamicInvoke(arguments);
        }
    }
}
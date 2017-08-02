using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class DelegateFieldModelBinderResolver : IFieldResolver
    {
        private readonly Delegate _resolver;

        public DelegateFieldModelBinderResolver(Delegate resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException("A resolver function must be specified");
        }

        public object Resolve(ResolveFieldContext context)
        {
            var parameters = _resolver.GetMethodInfo().GetParameters();

            int index = 0;
            object[] arguments = null;

            if (parameters.Any())
            {
                arguments = new object[parameters.Length];

                if (typeof(ResolveFieldContext) == parameters[index].ParameterType)
                {
                    arguments[index] = context;
                    index++;
                }

                if (context.Source?.GetType() == parameters[index].ParameterType)
                {
                    arguments[index] = context.Source;
                    index++;
                }

                foreach (var parameter in parameters.Skip(index))
                {
                    arguments[index] = context.GetArgument(parameter.ParameterType, parameter.Name, null);
                    index++;
                }
            }

            return _resolver.DynamicInvoke(arguments);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
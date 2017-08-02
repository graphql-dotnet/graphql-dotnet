using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class DelegateFieldModelBinderResolver<TSourceType, TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Delegate _resolver;

        public DelegateFieldModelBinderResolver(Delegate resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("A resolver function must be specified");
            }
            _resolver = resolver;
        }

        public async Task<TReturnType> Resolve(ResolveFieldContext context)
        {
            var parameters = _resolver.GetMethodInfo().GetParameters();

            int index = 0;
            object[] arguments = null;

            if (parameters.Any())
            {
                arguments = new object[parameters.Length];
                if (context.As<TSourceType>().GetType() == parameters[0].ParameterType)
                {
                    arguments[index] = context.As<TSourceType>();
                    index++;
                }

                foreach (var parameter in parameters.Skip(index))
                {
                    arguments[index] = context.GetArgument(parameter.ParameterType, parameter.Name, null);
                    index++;
                }
            }

            var result = _resolver.DynamicInvoke(arguments);

            if (result is Task)
            {
                var task = result as Task;
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }
                await task.ConfigureAwait(false);
                result = task.GetProperyValue("Result");
            }

            return (TReturnType)result;
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
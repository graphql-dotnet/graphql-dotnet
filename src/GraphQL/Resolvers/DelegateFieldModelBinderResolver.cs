using System;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Reflection;
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

        public async Task<object> ResolveAsync(IResolveFieldContext context)
        {
            var arguments = ReflectionHelper.BuildArguments(_parameters, context);
            var ret = _resolver.DynamicInvoke(arguments);
            if (ret is Task task)
            {
                await task.ConfigureAwait(false);
                return task.GetResult();
            }
            return ret;
        }
    }
}

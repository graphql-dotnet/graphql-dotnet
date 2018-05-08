using System;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class DelegateFieldModelBinderResolver : ReflectionFieldResolverBase
    {
        private readonly ParameterInfo[] _parameters;
        private readonly Delegate _resolver;

        public DelegateFieldModelBinderResolver(Delegate resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver),
                            "A resolver function must be specified");
            _parameters = _resolver.GetMethodInfo().GetParameters();
        }

        public override object Resolve(ResolveFieldContext context)
        {
            var arguments = BuildArguments(_parameters, context);
            return _resolver.DynamicInvoke(arguments);
        }
    }
}

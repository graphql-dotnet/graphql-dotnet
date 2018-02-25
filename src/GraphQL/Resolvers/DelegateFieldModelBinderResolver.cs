using System;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class DelegateFieldModelBinderResolver : FieldResolverBase
    {
        private readonly Delegate _resolver;
        private readonly ParameterInfo[] _parameters;

        public DelegateFieldModelBinderResolver(Delegate resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
            _parameters = _resolver.GetMethodInfo().GetParameters();
        }

        public override object Resolve(ResolveFieldContext context)
        {
            var arguments = BuildArguments(_parameters, context);
            return _resolver.DynamicInvoke(arguments);
        }
    }
}

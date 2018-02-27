using System;
using System.Linq;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Reflection
{
    internal class AccessorFieldResolver : FieldResolverBase
    {
        private readonly IAccessor _accessor;
        private readonly IDependencyResolver _dependencyResolver;

        public AccessorFieldResolver(IAccessor accessor, IDependencyResolver dependencyResolver)
        {
            _accessor = accessor;
            _dependencyResolver = dependencyResolver;
        }

        public override object Resolve(ResolveFieldContext context)
        {
            var arguments = BuildArguments(_accessor.Parameters, context);

            var target = _accessor.DeclaringType.IsInstanceOfType(context.Source)
                    ? context.Source
                    : _dependencyResolver.Resolve(_accessor.DeclaringType);

            if (target == null)
            {
                var parentType = context.ParentType != null ? $"{context.ParentType.Name}." : null;
                throw new InvalidOperationException($"Could not resolve an instance of {_accessor.DeclaringType.Name} to execute {parentType}{context.FieldName}");
            }

            return _accessor.GetValue(target, arguments);
        }
    }
}

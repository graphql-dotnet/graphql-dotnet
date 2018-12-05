using System;
using System.Reflection;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;

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
            var arguments = ReflectionHelper.BuildArguments(_accessor.Parameters, context);

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

        public object Resolve(ExecutionContext context, ExecutionNode node)
        {
            var resolveContext = context.CreateResolveFieldContext(node);
            return Resolve(resolveContext);
        }
    }
}

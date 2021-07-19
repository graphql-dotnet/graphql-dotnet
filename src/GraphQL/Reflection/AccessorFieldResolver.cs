using System;
using GraphQL.Resolvers;

namespace GraphQL.Reflection
{
    internal class AccessorFieldResolver : IFieldResolver
    {
        private readonly IAccessor _accessor;
        private readonly IServiceProvider _serviceProvider;

        public AccessorFieldResolver(IAccessor accessor, IServiceProvider serviceProvider)
        {
            _accessor = accessor;
            _serviceProvider = serviceProvider;
        }

        public object? Resolve(IResolveFieldContext context)
        {
            var arguments = ReflectionHelper.BuildArguments(_accessor.Parameters, context);

            var target = _accessor.DeclaringType.IsInstanceOfType(context.Source)
                    ? context.Source
                    : _serviceProvider.GetService(_accessor.DeclaringType);

            if (target == null)
            {
                var parentType = context.ParentType != null ? $"{context.ParentType.Name}." : null;
                throw new InvalidOperationException($"Could not resolve an instance of {_accessor.DeclaringType.Name} to execute {parentType}{context.FieldAst.Name}");
            }

            return _accessor.GetValue(target, arguments);
        }
    }
}

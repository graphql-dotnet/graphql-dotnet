using System;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Reflection
{
    internal class AccessorFieldResolver : IFieldResolverInternal
    {
        private readonly IAccessor _accessor;
        private readonly IServiceProvider _serviceProvider;

        public AccessorFieldResolver(IAccessor accessor, IServiceProvider serviceProvider)
        {
            _accessor = accessor;
            _serviceProvider = serviceProvider;
        }

        public Task SetResultAsync(IResolveFieldContext context)
        {
            var arguments = ReflectionHelper.BuildArguments(_accessor.Parameters, context);

            var target = _accessor.DeclaringType.IsInstanceOfType(context.Source)
                    ? context.Source
                    : _serviceProvider.GetService(_accessor.DeclaringType);

            if (target == null)
            {
                var parentType = context.ParentType != null ? $"{context.ParentType.Name}." : null;
                throw new InvalidOperationException($"Could not resolve an instance of {_accessor.DeclaringType.Name} to execute {parentType}{context.FieldName}");
            }

            context.Result = _accessor.GetValue(target, arguments);
            return Task.CompletedTask;
        }
    }
}

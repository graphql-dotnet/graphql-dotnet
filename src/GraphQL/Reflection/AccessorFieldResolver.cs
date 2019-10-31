using System;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

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

        public async Task<object> ResolveAsync(ResolveFieldContext context)
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

            var value = _accessor.GetValue(target, arguments);
            if (value is Task task)
            {
                await task.ConfigureAwait(false);
                return task.GetResult();
            }
            return value;
        }
    }
}

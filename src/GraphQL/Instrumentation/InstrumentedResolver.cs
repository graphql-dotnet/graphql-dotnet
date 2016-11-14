using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public class FieldResolverBase : IFieldResolver<Task<object>>
    {
        public IFieldResolver Inner { get; }

        public FieldResolverBase(IFieldResolver inner)
        {
            Inner = inner ?? new NameFieldResolver();
        }

        public virtual async Task<object> Resolve(ResolveFieldContext context)
        {
            var result = Inner.Resolve(context);

            if (result is Task)
            {
                var task = result as Task;
                await task.ConfigureAwait(false);
                result = task.GetProperyValue("Result");
            }

            return result;
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class InstrumentedResolver : FieldResolverBase
    {
        public InstrumentedResolver(IFieldResolver inner)
            : base(inner)
        {
        }

        public override Task<object> Resolve(ResolveFieldContext context)
        {
            var metadata = new Dictionary<string, object>
            {
                {"typeName", context.ParentType.Name},
                {"fieldName", context.FieldName}
            };

            using (context.Metrics.Subject("field", context.FieldName, metadata))
            {
                return base.Resolve(context);
            }
        }
    }
}

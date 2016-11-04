using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public class InstrumentedResolver : IFieldResolver<Task<object>>
    {
        private readonly Timings _timings;
        private readonly IFieldResolver _next;

        public InstrumentedResolver(IFieldResolver next, Timings timings)
        {
            _timings = timings;
            _next = next ?? new NameFieldResolver();
        }

        public async Task<object> Resolve(ResolveFieldContext context)
        {
            var metadata = new Dictionary<string, object>
            {
                {"typeName", context.ParentType.Name},
                {"fieldName", context.FieldName}
            };

            using (_timings.Subject("field", context.FieldName, metadata))
            {
                var result = _next.Resolve(context);

                if (result is Task)
                {
                    var task = result as Task;
                    await task.ConfigureAwait(false);
                    result = task.GetProperyValue("Result");
                }

                return result;
            }
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}

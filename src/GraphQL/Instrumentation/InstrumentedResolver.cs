using System.Linq;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public class InstrumentedResolver : IFieldResolver
    {
        private readonly Timings _timings;
        private readonly IFieldResolver _next;

        public InstrumentedResolver(IFieldResolver next, Timings timings)
        {
            _timings = timings;
            _next = next ?? new NameFieldResolver();
        }

        public object Resolve(ResolveFieldContext context)
        {
            bool isObject = context.FieldAst.SelectionSet != null && context.FieldAst.SelectionSet.Selections.Any();
            var type = isObject ? "object" : "field";

            using (_timings.Subject(type, context.FieldName))
            {
                var result = _next.Resolve(context);

                if (result is Task)
                {
                    var task = result as Task;
                    Task.WaitAll(task);
                    result = task.GetProperyValue("Result");
                }

                return result;
            }
        }
    }
}

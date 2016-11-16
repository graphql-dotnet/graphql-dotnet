using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public delegate Task<object> FieldMiddlewareDelegate(ResolveFieldContext context);
}

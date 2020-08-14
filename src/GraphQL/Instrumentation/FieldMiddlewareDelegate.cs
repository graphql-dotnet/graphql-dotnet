using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    public delegate Task<object> FieldMiddlewareDelegate(IResolveFieldContext context);
}

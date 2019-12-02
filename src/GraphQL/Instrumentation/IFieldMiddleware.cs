using GraphQL.Types;
using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    public interface IFieldMiddleware
    {
        Task<object> Resolve(ResolveFieldContext context, FieldMiddlewareDelegate next);
    }
}

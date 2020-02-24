using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Interface for field middleware. It doesn’t have to be implemented on your middleware.
    /// Then a search will be made for such a method with a suitable signature. Nevertheless,
    /// to improve performance, it is recommended to implement this interface.
    /// </summary>
    public interface IFieldMiddleware
    {
        Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next);
    }
}

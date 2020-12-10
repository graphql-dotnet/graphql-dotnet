using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Interface for field middleware. It doesn’t have to be implemented on your middleware.
    /// Then a search will be made for such a method with a suitable signature. Nevertheless,
    /// to improve performance, it is recommended to implement this interface.
    /// </summary>
    public interface IFieldMiddleware
    {
        /// <summary>
        /// Handles execution of a field.
        /// </summary>
        /// <param name="context">Contains parameters pertaining to the currently executing field.</param>
        /// <param name="next">The delegate representing the remaining middleware and field resolver in the pipeline.</param>
        /// <returns>Asynchronously returns the result for the field.</returns>
        Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next);
    }
}

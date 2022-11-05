namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Interface for field middleware.
    /// </summary>
    public interface IFieldMiddleware
    {
        /// <summary>
        /// Handles execution of a field.
        /// </summary>
        /// <param name="context">Contains parameters pertaining to the currently executing field.</param>
        /// <param name="next">The delegate representing the remaining middleware and field resolver in the pipeline.</param>
        /// <returns>Asynchronously returns the result for the field.</returns>
        ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next);
    }
}

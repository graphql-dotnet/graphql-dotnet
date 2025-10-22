namespace GraphQL.Instrumentation;

/// <summary>
/// Field middleware implementation that wraps a delegate function.
/// </summary>
internal sealed class FuncFieldMiddleware : IFieldMiddleware
{
    private readonly Func<IResolveFieldContext, FieldMiddlewareDelegate, ValueTask<object?>> _middleware;

    /// <summary>
    /// Initializes a new instance with the specified middleware delegate.
    /// </summary>
    /// <param name="middleware">The middleware delegate to wrap.</param>
    public FuncFieldMiddleware(Func<IResolveFieldContext, FieldMiddlewareDelegate, ValueTask<object?>> middleware)
    {
        _middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
    }

    /// <inheritdoc/>
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        return _middleware(context, next);
    }
}

using GraphQL.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// Field middleware that creates a dependency injection scope for the duration of the field resolver's execution.
/// </summary>
public class ScopedFieldMiddleware : IFieldMiddleware
{
    /// <summary>
    /// Singleton instance of the scoped field middleware.
    /// </summary>
    public static readonly ScopedFieldMiddleware Instance = new(null);

    private readonly IFieldMiddleware? _innerMiddleware;

    /// <summary>
    /// Initializes a new instance of <see cref="ScopedFieldMiddleware"/>.
    /// </summary>
    /// <param name="innerMiddleware">Optional inner middleware to execute before creating the scope.</param>
    public ScopedFieldMiddleware(IFieldMiddleware? innerMiddleware)
    {
        _innerMiddleware = innerMiddleware;
    }

    /// <inheritdoc/>
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        using var scope = context.RequestServicesOrThrow().CreateScope();
        var scopedContext = new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider);

        if (_innerMiddleware != null)
        {
            return await _innerMiddleware.ResolveAsync(scopedContext, next).ConfigureAwait(false);
        }
        else
        {
            return await next(scopedContext).ConfigureAwait(false);
        }
    }
}

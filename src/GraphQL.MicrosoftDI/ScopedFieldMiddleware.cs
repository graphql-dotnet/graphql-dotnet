using GraphQL.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// Field middleware that creates a dependency injection scope for the duration of the field resolver's execution.
/// </summary>
public sealed class ScopedFieldMiddleware : IFieldMiddleware
{
    /// <summary>
    /// Singleton instance of the scoped field middleware.
    /// </summary>
    public static readonly ScopedFieldMiddleware Instance = new();

    private ScopedFieldMiddleware() { }

    /// <inheritdoc/>
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        using var scope = context.RequestServicesOrThrow().CreateScope();
        var scopedContext = new ScopedResolveFieldContextAdapter<object>(context, scope.ServiceProvider);
        return await next(scopedContext).ConfigureAwait(false);
    }
}

using System.Diagnostics;

namespace GraphQL.Execution;

/// <summary>
/// Default implementation of <see cref="IResolveFieldContextAccessor"/> that uses <see cref="AsyncLocal{T}"/>
/// to store the current <see cref="IResolveFieldContext"/>.
/// </summary>
public class ResolveFieldContextAccessor : IResolveFieldContextAccessor
{
    private static readonly AsyncLocal<IResolveFieldContext?> _context = new();

    /// <inheritdoc/>
    [AllowNull]
    public IResolveFieldContext Context
    {
        get => _context.Value ?? ThrowNoContext();
        set => _context.Value = value;
    }

    [DoesNotReturn]
    [StackTraceHidden]
    private static IResolveFieldContext ThrowNoContext()
    {
        throw new InvalidOperationException("No IResolveFieldContext is currently set. Ensure that Schema.ResolveFieldContextAccessor is set and that this code is being called during field resolution.");
    }
}

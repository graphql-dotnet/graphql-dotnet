namespace GraphQL.Execution;

/// <summary>
/// Default implementation of <see cref="IResolveFieldContextAccessor"/> that uses <see cref="AsyncLocal{T}"/>
/// to store the current <see cref="IResolveFieldContext"/>.
/// </summary>
public sealed class ResolveFieldContextAccessor : IResolveFieldContextAccessor
{
    private static readonly AsyncLocal<IResolveFieldContext?> _context = new();
    private ResolveFieldContextAccessor() { }

    /// <summary>
    /// Singleton instance of the <see cref="ResolveFieldContextAccessor"/>.
    /// </summary>
    public static ResolveFieldContextAccessor Instance { get; } = new();

    /// <inheritdoc/>
    public IResolveFieldContext? Context
    {
        get => _context.Value;
        set => _context.Value = value;
    }
}

namespace GraphQL.Federation;

/// <summary>
/// FuncFederationResolver.
/// </summary>
public class FuncFederationResolver<TSourceType> : IFederationResolver
{
    private readonly Func<IResolveFieldContext, TSourceType, object?> _resolve;

    /// <inheritdoc/>
    public Type SourceType => typeof(TSourceType);

    /// <summary>
    /// .ctor
    /// </summary>
    public FuncFederationResolver(Func<IResolveFieldContext, TSourceType, object?> resolve)
    {
        _resolve = resolve;
    }

    /// <inheritdoc/>
    public object? Resolve(IResolveFieldContext context, object source) => _resolve(context, (TSourceType)source);
}

using GraphQL.Execution;

namespace GraphQL.Resolvers;

/// <summary>
/// Resolver wrapper that populates the <see cref="IResolveFieldContextAccessor"/> with the current context
/// before delegating to the wrapped resolver.
/// </summary>
internal class ResolveFieldContextAccessorStreamResolver : ISourceStreamResolver
{
    private readonly IResolveFieldContextAccessor _accessor;
    private readonly ISourceStreamResolver _innerResolver;

    public ResolveFieldContextAccessorStreamResolver(IResolveFieldContextAccessor accessor, ISourceStreamResolver innerResolver)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _innerResolver = innerResolver ?? throw new ArgumentNullException(nameof(innerResolver));
    }

    public async ValueTask<IObservable<object?>> ResolveAsync(IResolveFieldContext context)
    {
        _accessor.Context = context;
        IObservable<object?> ret;
        try
        {
            ret = await _innerResolver.ResolveAsync(context).ConfigureAwait(false);
        }
        finally
        {
            _accessor.Context = null;
        }
        return ret;
    }
}

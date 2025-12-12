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
        IObservable<object?> source;
        try
        {
            source = await _innerResolver.ResolveAsync(context).ConfigureAwait(false);
        }
        finally
        {
            _accessor.Context = null;
        }

        // Wrap the observable to set context during call to subscribe
        return new ContextAccessorObservable(_accessor, context, source);
    }

    private sealed class ContextAccessorObservable : IObservable<object?>
    {
        private readonly IResolveFieldContextAccessor _accessor;
        private readonly IResolveFieldContext _context;
        private readonly IObservable<object?> _source;

        public ContextAccessorObservable(IResolveFieldContextAccessor accessor, IResolveFieldContext context, IObservable<object?> source)
        {
            _accessor = accessor;
            _context = context;
            _source = source;
        }

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            _accessor.Context = _context;
            try
            {
                return _source.Subscribe(observer);
            }
            finally
            {
                _accessor.Context = null;
            }
        }
    }
}

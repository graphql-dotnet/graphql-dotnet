using GraphQL.Execution;

namespace GraphQL.Resolvers;

/// <summary>
/// Resolver wrapper that populates the <see cref="IResolveFieldContextAccessor"/> with the current context
/// before delegating to the wrapped resolver.
/// </summary>
internal sealed class ResolveFieldContextAccessorStreamResolver : ISourceStreamResolver
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
        // The following code ensures that the context is set in the correct ExecutionContext without clearing _accessor.Context
        //   in case asynchronous operations execute in the background that needs access to it.

        ValueTask<IObservable<object?>> task = default;
        using var capturedContext = System.Threading.ExecutionContext.Capture();
        if (capturedContext != null)
            System.Threading.ExecutionContext.Run(capturedContext, Inner, null);
        else
            Inner(null);

        void Inner(object? state)
        {
            _accessor.Context = context;
            task = _innerResolver.ResolveAsync(context);
        }

        var source = await task.ConfigureAwait(false);

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
            // The following code ensures that the context is set in the correct ExecutionContext without clearing _accessor.Context
            //   in case asynchronous operations execute in the background that needs access to it.

            IDisposable ret = null!;
            using var capturedContext = System.Threading.ExecutionContext.Capture();
            if (capturedContext != null)
                System.Threading.ExecutionContext.Run(capturedContext, Inner, null);
            else
                Inner(null);

            void Inner(object? state)
            {
                _accessor.Context = _context;
                ret = _source.Subscribe(observer);
            }

            return ret;
        }
    }
}

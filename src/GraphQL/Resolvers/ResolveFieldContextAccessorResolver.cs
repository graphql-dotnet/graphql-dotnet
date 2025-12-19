using GraphQL.DataLoader;
using GraphQL.Execution;

namespace GraphQL.Resolvers;

/// <summary>
/// Resolver wrapper that populates the <see cref="IResolveFieldContextAccessor"/> with the current context
/// before delegating to the wrapped resolver.
/// </summary>
internal class ResolveFieldContextAccessorResolver : IFieldResolver
{
    private readonly IResolveFieldContextAccessor _accessor;
    private readonly IFieldResolver _innerResolver;

    public ResolveFieldContextAccessorResolver(IResolveFieldContextAccessor accessor, IFieldResolver innerResolver)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _innerResolver = innerResolver ?? throw new ArgumentNullException(nameof(innerResolver));
    }

    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        _accessor.Context = context;
        try
        {
            var result = await _innerResolver.ResolveAsync(context).ConfigureAwait(false);

            // If the result is a data loader, wrap it to maintain context during execution
            if (result is IDataLoaderResult dataLoaderResult)
            {
                return new ContextPreservingDataLoaderResult(_accessor, context, dataLoaderResult);
            }

            return result;
        }
        finally
        {
            _accessor.Context = null;
        }
    }

    /// <summary>
    /// Wraps an IDataLoaderResult to ensure the context is set before calling GetResultAsync
    /// </summary>
    private sealed class ContextPreservingDataLoaderResult : IDataLoaderResult
    {
        private readonly IResolveFieldContextAccessor _accessor;
        private readonly IResolveFieldContext _context;
        private readonly IDataLoaderResult _innerResult;

        public ContextPreservingDataLoaderResult(
            IResolveFieldContextAccessor accessor,
            IResolveFieldContext context,
            IDataLoaderResult innerResult)
        {
            _accessor = accessor;
            _context = context;
            _innerResult = innerResult;
        }

        public async Task<object?> GetResultAsync(CancellationToken cancellationToken = default)
        {
            _accessor.Context = _context;
            try
            {
                return await _innerResult.GetResultAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _accessor.Context = null;
            }
        }
    }
}

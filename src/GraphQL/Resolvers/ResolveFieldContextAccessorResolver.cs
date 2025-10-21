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
        object? ret = null;
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Federation;

namespace GraphQL.Utilities.Federation;

#pragma warning disable CS0618 // Type or member is obsolete
public class FuncFederatedResolver<T> : IFederatedResolver, IFederationResolver
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly Func<FederatedResolveContext, Task<T?>> _resolver;

    public FuncFederatedResolver(Func<FederatedResolveContext, Task<T?>> func)
    {
        _resolver = func;
    }

    public Type SourceType => typeof(Dictionary<string, object?>);

    [Obsolete("Please use ResolveAsync instead. This method will be removed in v9.")]
    public async Task<object?> Resolve(FederatedResolveContext context)
    {
        return await _resolver(context).ConfigureAwait(false);
    }

    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, object source)
    {
        var federatedContext = new FederatedResolveContext
        {
            ParentFieldContext = context,
            Arguments = (Dictionary<string, object?>)source,
        };
        return await _resolver(federatedContext).ConfigureAwait(false);
    }
}

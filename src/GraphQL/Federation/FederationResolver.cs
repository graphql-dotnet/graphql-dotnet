using GraphQL.DataLoader;

namespace GraphQL.Federation;

internal class FederationResolver<TSourceType> : IFederationResolver
{
    private readonly Func<IResolveFieldContext, TSourceType, ValueTask<object?>> _resolveFunc;

    public Type SourceType => typeof(TSourceType);

    public FederationResolver(Func<IResolveFieldContext, TSourceType, TSourceType?> resolveFunc)
    {
        _resolveFunc = (ctx, source) => new(resolveFunc(ctx, source));
    }

    public FederationResolver(Func<IResolveFieldContext, TSourceType, Task<TSourceType?>> resolveFunc)
    {
        _resolveFunc = async (ctx, source) => (await resolveFunc(ctx, source).ConfigureAwait(false))!;
    }

    public FederationResolver(Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType?>> resolveFunc)
    {
        _resolveFunc = (ctx, source) => new(resolveFunc(ctx, source));
    }

    public ValueTask<object?> ResolveAsync(IResolveFieldContext context, object source) => _resolveFunc(context, (TSourceType)source)!;
}

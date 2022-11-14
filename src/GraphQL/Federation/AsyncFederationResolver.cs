namespace GraphQL.Federation;

internal class AsyncFederationResolver<TSourceType> : IFederationResolver
{
    private readonly Func<IResolveFieldContext, TSourceType, Task<TSourceType>> _resolveFunc;

    public Type SourceType => typeof(TSourceType);

    public AsyncFederationResolver(Func<IResolveFieldContext, TSourceType, Task<TSourceType>> resolveFunc)
    {
        _resolveFunc = resolveFunc;
    }

    public object Resolve(IResolveFieldContext context, object source) => _resolveFunc(context, (TSourceType)source);
}

using GraphQL.DataLoader;

namespace GraphQL.Federation;

internal class DataLoaderFederationResolver<TSourceType> : IFederationResolver
{
    private readonly Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType>> _resolveFunc;

    public Type SourceType => typeof(TSourceType);

    public DataLoaderFederationResolver(Func<IResolveFieldContext, TSourceType, IDataLoaderResult<TSourceType>> resolveFunc)
    {
        _resolveFunc = resolveFunc;
    }

    public object Resolve(IResolveFieldContext context, object source) => _resolveFunc(context, (TSourceType)source);
}

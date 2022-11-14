namespace GraphQL.Federation;

internal class FederationResolver<TSourceType> : IFederationResolver
{
    private readonly Func<IResolveFieldContext, TSourceType, TSourceType> _resolveFunc;

    public Type SourceType => typeof(TSourceType);

    public FederationResolver(Func<IResolveFieldContext, TSourceType, TSourceType> resolveFunc)
    {
        _resolveFunc = resolveFunc;
    }

    public object Resolve(IResolveFieldContext context, object source) => _resolveFunc(context, (TSourceType)source)!;
}

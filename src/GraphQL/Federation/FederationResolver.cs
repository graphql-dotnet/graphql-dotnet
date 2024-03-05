using GraphQL.Types;

namespace GraphQL.Federation;

internal class FederationResolver<TSourceType> : IFederationResolver
{
    private readonly Func<IResolveFieldContext, TSourceType, TSourceType> _resolveFunc;

    public Type SourceType => typeof(TSourceType);

    public IInputObjectGraphType? SourceGraphType { get; set; }

    public FederationResolver(Func<IResolveFieldContext, TSourceType, TSourceType> resolveFunc)
    {
        _resolveFunc = resolveFunc;
    }

    public object Resolve(IResolveFieldContext context, object source) => _resolveFunc(context, (TSourceType)source)!;
}

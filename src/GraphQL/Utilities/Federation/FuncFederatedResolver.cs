#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Federation.Resolvers;

namespace GraphQL.Utilities.Federation;

[Obsolete("Please use the GraphQL.Federation.FederationResolver class instead. This class will be removed in v9.")]
public class FuncFederatedResolver<TReturn> : FederationResolver<Dictionary<string, object?>, TReturn>, IFederatedResolver
{
    public FuncFederatedResolver(Func<FederatedResolveContext, Task<TReturn?>> func)
        : base((context, source) => func(new FederatedResolveContext
        {
            ParentFieldContext = context,
            Arguments = source,
        }))
    {
    }

    public async Task<object?> Resolve(FederatedResolveContext context)
    {
        return await _resolveFunc(context.ParentFieldContext, context.Arguments).ConfigureAwait(false);
    }
}

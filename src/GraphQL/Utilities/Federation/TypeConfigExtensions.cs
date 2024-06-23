#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Federation;

namespace GraphQL.Utilities.Federation;

public static class TypeConfigExtensions
{
    [Obsolete("Please use another overload instead, found in the GraphQL.Federation namespace. This method will be removed in v9.")]
    public static void ResolveReferenceAsync<T>(this TypeConfig config, Func<FederatedResolveContext, Task<T?>> resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = new FuncFederatedResolver<T>(resolver);
    }

    [Obsolete("Please use ResolveReference instead, found in the GraphQL.Federation namespace. This method will be removed in v9.")]
    public static void ResolveReferenceAsync(this TypeConfig config, IFederatedResolver resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = new FuncFederatedResolver<object>(resolver.Resolve);
    }
}

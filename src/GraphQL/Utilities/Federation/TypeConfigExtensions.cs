#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Federation;
using GraphQL.Federation.Extensions;

namespace GraphQL.Utilities.Federation;

public static class TypeConfigExtensions
{
    public static void ResolveReferenceAsync<T>(this TypeConfig config, Func<FederatedResolveContext, Task<T?>> resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = new FuncFederatedResolver<T>(resolver);
    }

    [Obsolete("Please use ResolveReference instead. This method will be removed in v9.")]
    public static void ResolveReferenceAsync(this TypeConfig config, IFederatedResolver resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = new FuncFederatedResolver<object>(resolver.Resolve);
    }

    public static void ResolveReference(this TypeConfig config, IFederationResolver resolver)
    {
        config.Metadata[FederationHelper.RESOLVER_METADATA] = resolver;
    }
}

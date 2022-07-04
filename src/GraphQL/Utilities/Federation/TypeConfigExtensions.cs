#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace GraphQL.Utilities.Federation
{
    public static class TypeConfigExtensions
    {
        public static void ResolveReferenceAsync<T>(this TypeConfig config, Func<FederatedResolveContext, Task<T?>> resolver)
        {
            ResolveReferenceAsync(config, new FuncFederatedResolver<T>(resolver));
        }

        public static void ResolveReferenceAsync(this TypeConfig config, IFederatedResolver resolver)
        {
            config.Metadata[FederatedSchemaBuilder.RESOLVER_METADATA_FIELD] = resolver;
        }
    }
}

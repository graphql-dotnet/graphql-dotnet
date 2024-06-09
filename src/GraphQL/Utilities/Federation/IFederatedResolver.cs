#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace GraphQL.Utilities.Federation;

[Obsolete("Please use IFederationResolver instead. This interface will be removed in v9.")]
public interface IFederatedResolver
{
    Task<object?> Resolve(FederatedResolveContext context);
}

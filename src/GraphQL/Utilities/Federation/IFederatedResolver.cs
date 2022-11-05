#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace GraphQL.Utilities.Federation
{
    public interface IFederatedResolver
    {
        Task<object?> Resolve(FederatedResolveContext context);
    }
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace GraphQL.Utilities.Federation;

[Obsolete("This class will be removed in v9 as it is not needed with GraphQL.Federation.Resolvers.IFederationResolver.")]
public class FederatedResolveContext
{
    public IResolveFieldContext ParentFieldContext { get; set; } = null!;
    public Dictionary<string, object?> Arguments { get; set; } = null!;
}

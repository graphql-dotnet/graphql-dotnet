using GraphQL.Resolvers;

namespace GraphQL.Types;

/// <summary>
/// Represents a root field of a subscription graph type.
/// </summary>
public class SubscriptionRootFieldType : ObjectFieldType // TODO: inherit from ObjectFieldType or from FieldType ???
{
    /// <summary>
    /// Gets or sets a subscription resolver for the field.
    /// </summary>
    public ISourceStreamResolver? StreamResolver { get; set; }
}

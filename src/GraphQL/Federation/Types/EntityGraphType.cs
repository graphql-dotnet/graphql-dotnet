using GraphQL.Types;

namespace GraphQL.Federation.Types;

/// <summary>
/// Represents a union type for entities in GraphQL Federation.
/// Used for resolving entities across different subgraphs.
/// The name of this graph type is "_Entity".
/// </summary>
public class EntityGraphType : UnionGraphType
{
    /// <inheritdoc cref="EntityGraphType"/>
    public EntityGraphType()
    {
        Name = "_Entity";
    }
}

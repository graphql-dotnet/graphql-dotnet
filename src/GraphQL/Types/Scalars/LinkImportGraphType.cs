using GraphQL.Federation.Types;

namespace GraphQL.Types.Scalars;

/// <summary>
/// Represents a scalar type for link imports in GraphQL Federation.
/// Used to define imported directives.
/// The name of this graph type is "link__Import".
/// </summary>
public class LinkImportGraphType : AnyScalarGraphType
{
    /// <inheritdoc cref="LinkImportGraphType"/>
    public LinkImportGraphType()
    {
        Name = "link__Import";
    }
}

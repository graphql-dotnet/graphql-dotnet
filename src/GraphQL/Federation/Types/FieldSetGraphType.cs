using GraphQL.Types;

namespace GraphQL.Federation.Types;

/// <summary>
/// Represents a string type for federation field sets in GraphQL Federation.
/// Used to define sets of fields required for entity resolution.
/// The name of this graph type is "federation__FieldSet".
/// </summary>
public class FieldSetGraphType : StringGraphType
{
    /// <inheritdoc cref="FieldSetGraphType"/>
    public FieldSetGraphType()
    {
        Name = "federation__FieldSet";
    }
}

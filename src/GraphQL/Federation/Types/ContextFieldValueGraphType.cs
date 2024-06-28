using GraphQL.Types;

namespace GraphQL.Federation.Types;

/// <summary>
/// Represents a GraphQL scalar type used to define the value of a field from a specified context.
/// </summary>
public class ContextFieldValueGraphType : StringGraphType
{
    /// <inheritdoc cref="ContextFieldValueGraphType"/>
    public ContextFieldValueGraphType()
    {
        Name = "ContextFieldValue";
    }
}

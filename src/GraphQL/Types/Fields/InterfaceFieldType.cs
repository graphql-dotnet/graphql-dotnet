namespace GraphQL.Types;

/// <summary>
/// Represents a field of an interface graph type.
/// </summary>
public class InterfaceFieldType : FieldType, IFieldTypeWithArguments
{
    /// <inheritdoc/>
    public QueryArguments? Arguments { get; set; }
}

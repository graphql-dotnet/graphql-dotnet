namespace GraphQL.Types;

/// <summary>
/// Represents a field of an input graph type.
/// </summary>
public class InputFieldType : FieldType, IHaveDefaultValue
{
    /// <summary>
    /// Gets or sets the default value of the field. Only applies to fields of input object graph types.
    /// </summary>
    public object? DefaultValue { get; set; }
}

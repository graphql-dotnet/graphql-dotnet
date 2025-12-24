using GraphQL.Utilities;

namespace GraphQL.Types;

/// <summary>
/// Represents an argument of a directive applied to a schema element - type, field, argument, etc.
/// </summary>
public class DirectiveArgument
{
    /// <summary>
    /// Creates argument.
    /// </summary>
    /// <param name="name">Argument name.</param>
    public DirectiveArgument(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Argument name.
    /// </summary>
    public string Name
    {
        get;
        set
        {
            NameValidator.ValidateName(value, NamedElement.Argument);
            field = value;
        }
    } = null!;

    /// <summary>
    /// Argument value.
    /// </summary>
    public object? Value { get; set; }
}

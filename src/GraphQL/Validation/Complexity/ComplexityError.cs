namespace GraphQL.Validation.Complexity;

/// <summary>
/// Represents a complexity error.
/// </summary>
public class ComplexityError : ValidationError
{
    /// <inheritdoc cref="ComplexityError"/>
    public ComplexityError(string message) : base(message)
    {
    }
}

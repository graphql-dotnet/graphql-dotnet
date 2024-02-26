namespace GraphQL.Validation;

/// <summary>
/// Represents a validation rule for a document.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Prepares and returns a node visitor to be used to validate a document (via a node walker) against this
    /// validation rule before the document has parsed any arguments. Validation failures are added then by this
    /// visitor to a list stored within <see cref="ValidationContext.Errors"/>.
    /// </summary>
    ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context);

    /// <summary>
    /// Prepares and returns a visitor which methods are called when parsing the inputs into variables in
    /// <see cref="ValidationContext.GetVariablesValuesAsync(IVariableVisitor?)"/>.
    /// </summary>
    ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context);

    /// <summary>
    /// Prepares and returns a node visitor to be used to validate a document (via a node walker) against this
    /// validation rule after the document has parsed all arguments. Validation failures are added then by this
    /// visitor to a list stored within <see cref="ValidationContext.Errors"/>.
    /// </summary>
    ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context);
}

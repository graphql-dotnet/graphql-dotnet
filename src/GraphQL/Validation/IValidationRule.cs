namespace GraphQL.Validation
{
    /// <summary>
    /// Represents a validation rule for a document.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Prepares and returns a node visitor to be used to validate a document (via a node walker) against this
        /// validation rule. Validation failures are added then by this visitor to a list stored within <see cref="ValidationContext.Errors"/>.
        /// </summary>
        ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context);
    }
}

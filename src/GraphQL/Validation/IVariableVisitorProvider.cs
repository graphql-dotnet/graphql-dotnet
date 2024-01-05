namespace GraphQL.Validation
{
    /// <summary>
    /// An interface for an object that provides <see cref="IVariableVisitor"/>.
    /// In most cases, this interface should be implemented by validation rules.
    /// </summary>
    public interface IVariableVisitorProvider
    {
        /// <summary>
        /// Gets a visitor for the specified validation context.
        /// </summary>
        IVariableVisitor? GetVisitor(ValidationContext context);

        /// <summary>
        /// Prepares and returns a node visitor to be used to validate a document (via a node walker) against this
        /// validation rule after the document has parsed all arguments. Validation failures are added then by this
        /// visitor to a list stored within <see cref="ValidationContext.Errors"/>.
        /// </summary>
        ValueTask<INodeVisitor?> ValidateArgumentsAsync(ValidationContext context);
    }
}

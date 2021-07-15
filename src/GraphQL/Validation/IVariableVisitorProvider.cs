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
        IVariableVisitor GetVisitor(ValidationContext context);
    }
}

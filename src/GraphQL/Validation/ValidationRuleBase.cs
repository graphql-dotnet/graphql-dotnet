namespace GraphQL.Validation;

/// <summary>
/// Base implementation of a validation rule with no functionality.
/// </summary>
public abstract class ValidationRuleBase : IValidationRule
{
    /// <inheritdoc/>
    public virtual ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => default;

    /// <inheritdoc/>
    public virtual ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context) => default;

    /// <inheritdoc/>
    public virtual ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context) => default;
}

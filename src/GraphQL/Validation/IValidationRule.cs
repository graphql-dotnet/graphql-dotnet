namespace GraphQL.Validation
{
    public interface IValidationRule
    {
        INodeVisitor Validate(ValidationContext context);
    }
}

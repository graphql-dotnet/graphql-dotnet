namespace GraphQL.Validation.Complexity;

public class ComplexityValidationRule : IValidationRule
{
    private readonly ComplexityConfiguration _complexityConfiguration;

    public ComplexityValidationRule(ComplexityConfiguration complexityConfiguration)
    {
        _complexityConfiguration = complexityConfiguration;
    }

    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {

    }
}

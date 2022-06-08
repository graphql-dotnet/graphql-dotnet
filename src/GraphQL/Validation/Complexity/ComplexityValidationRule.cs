namespace GraphQL.Validation.Complexity;

/// <summary>
/// Analyzes a document to determine if its complexity exceeds a threshold.
/// </summary>
public class ComplexityValidationRule : IValidationRule
{
    private readonly ComplexityConfiguration _complexityConfiguration;
    private readonly ComplexityAnalyzer _complexityAnalyzer = new();

    /// <inheritdoc cref="ComplexityValidationRule"/>
    public ComplexityValidationRule(ComplexityConfiguration complexityConfiguration)
    {
        _complexityConfiguration = complexityConfiguration;
    }

    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        try
        {
            _complexityAnalyzer.Validate(context.Document, _complexityConfiguration, context.Schema);
        }
        catch (ComplexityError ex)
        {
            context.ReportError(ex);
        }
        return default;
    }
}

using GraphQL.Validation.Complexity;
using GraphQL.Validation.Errors.Custom;

namespace GraphQL.Validation.Rules.Custom;

/// <summary>
/// Analyzes a document to determine if its complexity exceeds a threshold.
/// </summary>
public class ComplexityValidationRule : IValidationRule
{
    private readonly ComplexityConfiguration _complexityConfiguration;
    private readonly IComplexityAnalyzer _complexityAnalyzer;

    /// <summary>
    /// Initializes an instance with the specified complexity configuration.
    /// </summary>
    public ComplexityValidationRule(ComplexityConfiguration complexityConfiguration)
        : this(complexityConfiguration, new ComplexityAnalyzer())
    {
    }

    /// <summary>
    /// Initializes an instance with the specified complexity configuration and complexity analyzer.
    /// </summary>
    public ComplexityValidationRule(ComplexityConfiguration complexityConfiguration, IComplexityAnalyzer complexityAnalyzer)
    {
        _complexityConfiguration = complexityConfiguration;
        _complexityAnalyzer = complexityAnalyzer;
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

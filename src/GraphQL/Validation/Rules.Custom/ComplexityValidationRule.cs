using GraphQL.Types;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Errors.Custom;

namespace GraphQL.Validation.Rules.Custom;

/// <summary>
/// Analyzes a document to determine if its complexity exceeds a threshold.
/// </summary>
public class ComplexityValidationRule : ValidationRuleBase
{
    /// <inheritdoc cref="Complexity.ComplexityConfiguration"/>
    protected ComplexityConfiguration ComplexityConfiguration { get; }

    /// <summary>
    /// Initializes an instance with the specified complexity configuration.
    /// </summary>
    public ComplexityValidationRule(ComplexityConfiguration complexityConfiguration)
    {
        ComplexityConfiguration = complexityConfiguration;
    }

    /// <inheritdoc/>
    public override async ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
    {
        // Fast return here to avoid all possible problems with complexity analysis.
        // For example, document may contain fragment cycles, see https://github.com/graphql-dotnet/graphql-dotnet/issues/3527
        if (!context.HasErrors)
            using (context.Metrics.Subject("document", "Analyzing complexity"))
            {
                var complexity = await CalculateComplexityAsync(context).ConfigureAwait(false);
                await ValidateComplexityAsync(context, complexity.TotalComplexity, complexity.MaximumDepth).ConfigureAwait(false);
            }
        return default;
    }

    /// <summary>
    /// Visits the operation specified by <see cref="ValidationContext.Operation"/> to determine its Total Complexity and Maximum Depth.
    /// <para>
    /// Total Complexity is the total complexity of the query, calculated by summing the complexity of each field. Each field's complexity
    /// is multiplied by all of its parent fields' child impact multipliers.
    /// </para>
    /// <para>
    /// Maximum Depth is the maximum depth of the query, calculated by counting the number of nested fields.
    /// </para>
    /// <para>
    /// To determine the complexity of a field, the field's complexity delegate is pulled from the field's metadata (see
    /// <see cref="ComplexityAnalayzerMetadataExtensions.GetComplexityImpactDelegate(FieldType)">GetComplexityImpactFunc</see>).
    /// The complexity delegate is then called with a <see cref="FieldImpactContext"/> containing the field, the parent type, and the visitor context.
    /// The delegate returns a tuple containing the field's complexity and the child impact multiplier. The field's complexity is multiplied by the
    /// parent fields' child impact multipliers to determine the field's total complexity, and this is summed to determine the total complexity of the query.
    /// </para>
    /// <para>
    /// If no complexity delegate is found on a field, the default complexity delegate specified by <see cref="ComplexityConfiguration.DefaultComplexityImpactDelegate"/>
    /// is used. The default implementation computes the field impact as follows: <see cref="ComplexityConfiguration.DefaultScalarImpact"/> for scalar fields and
    /// <see cref="ComplexityConfiguration.DefaultObjectImpact"/> for object fields. The default implementation computes the child impact multiplier as follows:
    /// if the field is a list field, and has an integer 'first' or 'last' argument, the multiplier is the value of the argument. If the field is a list field
    /// and has a 'id' argument supplied, the multiplier is 1. If the field is a list field, and if the parent is not a list field and has a 'first' or 'last'
    /// argument, the multiplier is the value of the argument. Otherwise, the multiplier is <see cref="ComplexityConfiguration.DefaultListImpactMultiplier"/>.
    /// </para>
    /// </summary>
    protected virtual ValueTask<(double TotalComplexity, int MaximumDepth)> CalculateComplexityAsync(ValidationContext context)
        => ComplexityVisitor.RunAsync(context, ComplexityConfiguration);

    /// <summary>
    /// Determines if the computed complexity exceeds the configured threshold.
    /// The default implementation checks if the total complexity exceeds <see cref="ComplexityConfiguration.MaxComplexity"/>
    /// and if the maximum depth exceeds <see cref="ComplexityConfiguration.MaxDepth"/>. If either threshold is exceeded, a
    /// <see cref="ComplexityError"/> is reported. Otherwise, the <see cref="ComplexityConfiguration.ValidateComplexityDelegate"/>
    /// is called.
    /// </summary>
    protected virtual Task ValidateComplexityAsync(ValidationContext context, double totalComplexity, int maxDepth)
    {
        if (totalComplexity > ComplexityConfiguration.MaxComplexity)
            context.ReportError(new ComplexityError(
                $"Query is too complex to execute. Complexity is {totalComplexity}; maximum allowed on this endpoint is {ComplexityConfiguration.MaxComplexity}."));

        if (maxDepth > ComplexityConfiguration.MaxDepth)
            context.ReportError(new ComplexityError(
                $"Query is too nested to execute. Maximum depth is {maxDepth} levels; maximum allowed on this endpoint is {ComplexityConfiguration.MaxDepth}."));

        return !context.HasErrors && ComplexityConfiguration.ValidateComplexityDelegate != null
            ? ComplexityConfiguration.ValidateComplexityDelegate(context, totalComplexity, maxDepth)
            : Task.CompletedTask;
    }
}

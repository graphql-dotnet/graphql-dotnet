using GraphQL.Types;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Configuration parameters for a complexity analyzer.
/// </summary>
public class ComplexityConfiguration
{
    /// <summary>
    /// Gets or sets the allowed maximum depth of the query.
    /// <see langword="null"/> if the depth does not need to be limited.
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets the maximum calculated document complexity factor.
    /// <see langword="null"/> if the complexity does not need to be limited.
    /// </summary>
    public int? MaxComplexity { get; set; }

    /// <summary>
    /// Default complexity impact to use for scalar fields. Defaults to 1.
    /// </summary>
    public double DefaultScalarImpact { get; set; } = 1;

    /// <summary>
    /// Default complexity impact to use for object fields. Defaults to 1.
    /// </summary>
    public double DefaultObjectImpact { get; set; } = 1;

    /// <summary>
    /// Default child multiplier to use for list fields. Should represent the average number of rows/objects returned by each field.
    /// Defaults to 5.
    /// </summary>
    public double DefaultChildImpactMultiplier { get; set; } = 5;

    /// <summary>
    /// Validates the Total Complexity (double) and Maximum Depth (int) of the query against user-defined limits, such as per-user, per-IP or throttling limits.
    /// These delegate executes only after <see cref="MaxComplexity"/> and <see cref="MaxDepth"/> have been checked and are within limits.
    /// This delegate can also be used to log the complexity and depth of queries that pass limit checks.
    /// </summary>
    public Func<ValidationContext, double, int, Task>? ValidateComplexityDelegate { get; set; }

    /// <summary>
    /// The default complexity function to use when one is not defined on the field.
    /// The default implementation will check for first/last arguments on list fields, or their parents if their parents are non-list fields,
    /// and use the default scalar/object impact values.
    /// </summary>
    public Func<FieldImpactContext, (double FieldImpact, double ChildImpactMultiplier)> DefaultComplexityImpactDelegate { get; set; }
        = DefaultComplexityImpactDelegateImpl;

    /// <summary>
    /// The default complexity function.
    /// </summary>
    private static (double, double) DefaultComplexityImpactDelegateImpl(FieldImpactContext context)
    {
        double multiplier = 1;
        var graphType = context.FieldDefinition.ResolvedType;
        graphType = graphType is NonNullGraphType nonNullGraphType1 ? nonNullGraphType1.ResolvedType : graphType;
        while (graphType is ListGraphType listGraphType)
        {
            graphType = listGraphType.ResolvedType;
            graphType = graphType is NonNullGraphType nonNullGraphType2 ? nonNullGraphType2.ResolvedType : graphType;
            multiplier *= context.Configuration.DefaultChildImpactMultiplier;
        }
        var impact = graphType is ScalarGraphType ? context.Configuration.DefaultScalarImpact : context.Configuration.DefaultObjectImpact;

        var isList = context.FieldDefinition.ResolvedType is ListGraphType || context.FieldDefinition.ResolvedType is NonNullGraphType nonNullGraphType && nonNullGraphType.ResolvedType is ListGraphType;
        if (isList)
        {
            try
            {
                // if this is a list, check if the field has a first or last argument, or if an id is specified
                var rows = context.GetArgument<int?>("first");
                if (rows.HasValue)
                {
                    return (impact, rows.Value);
                }
                rows = context.GetArgument<int?>("last");
                if (rows.HasValue)
                {
                    return (impact, rows.Value);
                }
                if (context.GetArgument<object?>("id") != null)
                {
                    return (impact, 1);
                }
                // if not, and if the parent isn't a list, check if the parent has a first or last argument
                // (this is a common pattern for relay connection types)
                var parent = context.Parent;
                if (parent != null)
                {
                    isList = parent.Value.FieldDefinition.ResolvedType is ListGraphType || parent.Value.FieldDefinition.ResolvedType is NonNullGraphType parentNonNullGraphType && parentNonNullGraphType.ResolvedType is ListGraphType;
                    if (!isList)
                    {
                        rows = parent.Value.GetArgument<int?>("first");
                        if (rows.HasValue)
                        {
                            return (impact, rows.Value);
                        }
                        rows = parent.Value.GetArgument<int?>("last");
                        if (rows.HasValue)
                        {
                            return (impact, rows.Value);
                        }
                    }
                }
            }
            catch // first and last may represent arguments that are not integers, so we should ignore any exceptions
            {
            }
        }

        return (impact, multiplier);
    }
}

using GraphQL.Types;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Configuration parameters for a complexity analyzer.
/// </summary>
public class ComplexityOptions
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
    public double DefaultListImpactMultiplier { get; set; } = 20;

    /// <summary>
    /// Validates the Total Complexity (double) and Maximum Depth (int) of the query against user-defined limits, such as per-user, per-IP or throttling limits.
    /// These delegate executes only after <see cref="MaxComplexity"/> and <see cref="MaxDepth"/> have been checked and are within limits.
    /// This delegate can also be used to log the complexity and depth of queries that pass or fail limit checks.
    /// </summary>
    public Func<ComplexityValidationContext, Task>? ValidateComplexityDelegate { get; set; }

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
    private static (double FieldImapct, double ChildImpactMultiplier) DefaultComplexityImpactDelegateImpl(FieldImpactContext context)
    {
        // unwrap any list types and calculate the child impact multiplier
        // multiplier = DefaultListImpactMultiplier ^ (number of list types) -- e.g. 1 for non-list fields, 5 for list fields, 25 for lists of list fields, etc.
        double multiplier = 1;
        var graphType = context.FieldDefinition.ResolvedType;
        graphType = graphType is NonNullGraphType nonNullGraphType1 ? nonNullGraphType1.ResolvedType : graphType;
        while (graphType is ListGraphType listGraphType)
        {
            graphType = listGraphType.ResolvedType;
            graphType = graphType is NonNullGraphType nonNullGraphType2 ? nonNullGraphType2.ResolvedType : graphType;
            multiplier *= context.Configuration.DefaultListImpactMultiplier;
        }
        // calculate the impact
        var impact = graphType is ScalarGraphType
            ? context.Configuration.DefaultScalarImpact  // scalar fields
            : context.Configuration.DefaultObjectImpact; // object fields

        var isList = context.FieldDefinition.ResolvedType is ListGraphType || context.FieldDefinition.ResolvedType is NonNullGraphType nonNullGraphType && nonNullGraphType.ResolvedType is ListGraphType;
        if (isList)
        {
            // if this is a list, check if the field has a first or last argument (only IntGraphTypes are supported), or if an id is specified (any type)
            if (context.Arguments?.TryGetValue("first", out var arg) == true && arg.Value is int firstValue)
            {
                return (impact, firstValue);
            }
            if (context.Arguments?.TryGetValue("last", out arg) == true && arg.Value is int lastValue)
            {
                return (impact, lastValue);
            }
            if (context.Arguments?.TryGetValue("id", out _) == true)
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
                    if (parent.Value.Arguments?.TryGetValue("first", out arg) == true && arg.Value is int firstValue2)
                    {
                        return (impact, firstValue2);
                    }
                    if (parent.Value.Arguments?.TryGetValue("last", out arg) == true && arg.Value is int lastValue2)
                    {
                        return (impact, lastValue2);
                    }
                }
            }
        }

        return (impact, multiplier);
    }
}

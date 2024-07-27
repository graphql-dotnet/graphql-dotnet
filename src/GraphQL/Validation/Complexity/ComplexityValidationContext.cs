using GraphQL.Validation.Errors.Custom;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Provides contextual information for the complexity validation delegate;
/// see <see cref="ComplexityOptions.ValidateComplexityDelegate"/>.
/// <para>
/// Set or clear the <see cref="Error"/> property to control the validation result.
/// </para>
/// </summary>
public class ComplexityValidationContext
{
    /// <summary>
    /// Initializes a new instance with the specified parameters.
    /// </summary>
    public ComplexityValidationContext(
        ValidationContext validationContext,
        ComplexityOptions complexityOptions,
        double totalImpact,
        int maxDepth,
        ValidationError? error)
    {
        ValidationContext = validationContext;
        Configuration = complexityOptions;
        TotalImpact = totalImpact;
        MaxDepth = maxDepth;
        Error = error;
    }

    /// <inheritdoc cref="Validation.ValidationContext"/>
    public ValidationContext ValidationContext { get; }

    /// <summary>
    /// Returns the set of configured options for complexity analysis.
    /// </summary>
    public ComplexityOptions Configuration { get; }

    /// <summary>
    /// Returns the total computed complexity impact of the selected operation.
    /// </summary>
    public double TotalImpact { get; }

    /// <summary>
    /// Returns the maximum depth of the selected operation.
    /// </summary>
    public int MaxDepth { get; }

    /// <summary>
    /// Gets or sets an error that occurred due to complexity validation, or
    /// <see langword="null"/> when the document passes validation.
    /// <para>
    /// This is initially set to an instance of <see cref="ComplexityError"/>
    /// when the <see cref="TotalImpact"/> or <see cref="MaxDepth"/> exceeds the
    /// limits configured within <see cref="ComplexityOptions"/>.
    /// </para>
    /// </summary>
    public ValidationError? Error { get; set; }
}

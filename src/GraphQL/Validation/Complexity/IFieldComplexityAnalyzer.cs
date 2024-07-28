using GraphQL.Attributes;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Represents a field complexity analyzer. Used with the <see cref="ComplexityAttribute"/> to specify
/// a custom implementation for calculating the complexity of a field. Classes that implement this
/// interface must have a public parameterless constructor.
/// </summary>
public interface IFieldComplexityAnalyzer
{
    /// <summary>
    /// Analyzes the complexity of a field.
    /// </summary>
    FieldComplexityResult Analyze(FieldImpactContext context);
}

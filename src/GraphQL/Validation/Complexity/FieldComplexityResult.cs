namespace GraphQL.Validation.Complexity;

/// <summary>
/// Contains the result of the impact calculation for a specific field.
/// </summary>
public struct FieldComplexityResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldComplexityResult"/> struct.
    /// </summary>
    public FieldComplexityResult(double fieldImpact, double childImpactMultiplier)
    {
        FieldImpact = fieldImpact;
        ChildImpactMultiplier = childImpactMultiplier;
    }

    /// <summary>
    /// The field impact to add to the total complexity.
    /// Note that this value is multiplied by the current child impact modifier at this level of the document.
    /// </summary>
    public double FieldImpact;

    /// <summary>
    /// The child impact multiplier to apply to the child fields of this field.
    /// This value is multiplicatively applied to the child impact mulriplier of the parent field.
    /// </summary>
    public double ChildImpactMultiplier;
}

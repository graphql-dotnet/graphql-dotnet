using GraphQL.Types;
using GraphQL.Validation.Rules.Custom;

namespace GraphQL;

/// <summary>
/// Provides extension methods for working with field's complexity impact.
/// </summary>
public static class ComplexityAnalayzerMetadataExtensions
{
    private const string COMPLEXITY_IMPACT = "__COMPLEXITY_IMPACT__";

    /// <summary>
    /// Specify field's complexity impact which will be taken into account by <see cref="LegacyComplexityValidationRule"/>.
    /// </summary>
    /// <typeparam name="TMetadataProvider">The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining.</typeparam>
    /// <param name="provider">Metadata provider which must implement <see cref="IMetadataWriter"/> interface.</param>
    /// <param name="impact">Field's complexity impact.</param>
    /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
    public static TMetadataProvider WithComplexityImpact<TMetadataProvider>(this TMetadataProvider provider, double impact)
        where TMetadataProvider : IFieldMetadataWriter
        => provider.WithMetadata(COMPLEXITY_IMPACT, impact);

    /// <summary>
    /// Get field's complexity impact which will be taken into account by <see cref="LegacyComplexityValidationRule"/>.
    /// </summary>
    /// <param name="provider">Metadata provider which must implement <see cref="IProvideMetadata"/> interface.</param>
    /// <returns>Field's complexity impact.</returns>
    [Obsolete("Please use GetComplexityImpactDelegate instead.")]
    public static double? GetComplexityImpact(this IMetadataReader provider)
        => provider.GetMetadata<double?>(COMPLEXITY_IMPACT);
}

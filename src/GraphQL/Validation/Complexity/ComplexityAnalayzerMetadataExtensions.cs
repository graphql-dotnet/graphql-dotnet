using GraphQL.Types;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Provides extension methods for working with field's complexity impact.
/// </summary>
public static class ComplexityAnalayzerMetadataExtensions
{
    private const string COMPLEXITY_IMPACT = "__COMPLEXITY_IMPACT__";

    /// <summary>
    /// Specify field's complexity impact which will be taken into account by <see cref="ComplexityAnalyzer"/>.
    /// </summary>
    /// <typeparam name="TMetadataProvider">The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining.</typeparam>
    /// <param name="provider">Metadata provider which must implement <see cref="IProvideMetadata"/> interface.</param>
    /// <param name="impact">Field's complexity impact.</param>
    /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
    public static TMetadataProvider WithComplexityImpact<TMetadataProvider>(this TMetadataProvider provider, double impact)
        where TMetadataProvider : IProvideMetadata
        => provider.WithMetadata(COMPLEXITY_IMPACT, impact);

    /// <summary>
    /// Get field's complexity impact which will be taken into account by <see cref="ComplexityAnalyzer"/>.
    /// </summary>
    /// <param name="provider">Metadata provider which must implement <see cref="IProvideMetadata"/> interface.</param>
    /// <returns>Field's complexity impact.</returns>
    public static double? GetComplexityImpact(this IProvideMetadata provider)
        => provider.GetMetadata<double?>(COMPLEXITY_IMPACT);
}

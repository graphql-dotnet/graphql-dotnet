using GraphQL.Types;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Provides extension methods for working with field's impact.
/// </summary>
public static class ComplexityAnalayzerMetadataExtensions
{
    /// <summary>
    /// Specify field's impact which will be taken into account by <see cref="ComplexityAnalyzer"/>.
    /// </summary>
    /// <typeparam name="TMetadataProvider"> The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining. </typeparam>
    /// <param name="provider"> Metadata provider which must implement <see cref="IProvideMetadata"/> interface. </param>
    /// <param name="impact"> Field's impact. </param>
    /// <returns> The reference to the specified <paramref name="provider"/>. </returns>
    public static TMetadataProvider WithImpact<TMetadataProvider>(this TMetadataProvider provider, double impact)
        where TMetadataProvider : IProvideMetadata
        => provider.WithMetadata("impact", impact);

    /// <summary>
    /// Get field's impact which will be taken into account by <see cref="ComplexityAnalyzer"/>.
    /// </summary>
    /// <param name="provider"> Metadata provider which must implement <see cref="IProvideMetadata"/> interface. </param>
    /// <returns> Field's impact. </returns>
    public static double? GetImpact(this IProvideMetadata? provider)
        => provider?.GetMetadata<double?>("impact");
}

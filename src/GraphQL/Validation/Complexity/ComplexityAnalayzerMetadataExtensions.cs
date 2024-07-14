using GraphQL.Types;
using GraphQL.Validation.Rules.Custom;

namespace GraphQL;

/// <summary>
/// Provides extension methods for working with field's complexity impact.
/// </summary>
public static class ComplexityAnalayzerMetadataExtensions
{
    private const string COMPLEXITY_IMPACT = "__COMPLEXITY_IMPACT__";
    private const string COMPLEXITY_IMPACT_FUNC = "__COMPLEXITY_IMPACT_FUNC__";

    /// <summary>
    /// Specify field's complexity impact which will be taken into account by <see cref="LegacyComplexityValidationRule"/>.
    /// Changing this value does not affect the complexity impact of child fields.
    /// </summary>
    /// <typeparam name="TMetadataProvider">The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining.</typeparam>
    /// <param name="provider">Metadata provider which must implement <see cref="IMetadataWriter"/> interface.</param>
    /// <param name="impact">Field's complexity impact.</param>
    /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
    public static TMetadataProvider WithComplexityImpact<TMetadataProvider>(this TMetadataProvider provider, double impact)
        where TMetadataProvider : IFieldMetadataWriter
    {
        provider.WithMetadata(COMPLEXITY_IMPACT, impact);
        return provider.WithComplexityImpact(context => (impact, context.Configuration.DefaultComplexityImpactFunc(context).ChildImpactMultiplier));
    }

    /// <summary>
    /// Specify field's complexity impact which will be taken into account by <see cref="LegacyComplexityValidationRule"/>.
    /// </summary>
    /// <typeparam name="TMetadataProvider">The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining.</typeparam>
    /// <param name="provider">Metadata provider which must implement <see cref="IMetadataWriter"/> interface.</param>
    /// <param name="fieldImpact">Field's complexity impact.</param>
    /// <param name="childImpactMultiplier">Multiplier applied to child fields' complexity impact values.</param>
    /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
    public static TMetadataProvider WithComplexityImpact<TMetadataProvider>(this TMetadataProvider provider, double fieldImpact, double childImpactMultiplier)
        where TMetadataProvider : IFieldMetadataWriter
        => provider.WithComplexityImpact(_ => (fieldImpact, childImpactMultiplier));

    /// <summary>
    /// Specify field's complexity impact which will be taken into account by <see cref="LegacyComplexityValidationRule"/>.
    /// </summary>
    /// <typeparam name="TMetadataProvider">The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining.</typeparam>
    /// <param name="provider">Metadata provider which must implement <see cref="IMetadataWriter"/> interface.</param>
    /// <param name="func">A function which calculates the complexity impact of the field.</param>
    /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
    public static TMetadataProvider WithComplexityImpact<TMetadataProvider>(this TMetadataProvider provider, Func<NewComplexityValidationRule.FieldImpactContext, (double, double)> func)
        where TMetadataProvider : IFieldMetadataWriter
        => provider.WithMetadata(COMPLEXITY_IMPACT_FUNC, func);

    /// <summary>
    /// Get field's complexity impact which will be taken into account by <see cref="LegacyComplexityValidationRule"/>.
    /// </summary>
    /// <param name="provider">Metadata provider which must implement <see cref="IProvideMetadata"/> interface.</param>
    /// <returns>Field's complexity impact.</returns>
    [Obsolete("Please use GetComplexityImpactFunc instead.")]
    public static double? GetComplexityImpact(this IMetadataReader provider)
        => provider.GetMetadata<double?>(COMPLEXITY_IMPACT);

    /// <summary>
    /// Get field's complexity impact which will be taken into account by <see cref="LegacyComplexityValidationRule"/>.
    /// </summary>
    public static Func<NewComplexityValidationRule.FieldImpactContext, (double FieldImpact, double ChildImpactMultiplier)>? GetComplexityImpactFunc(this FieldType provider)
        => provider.GetMetadata<Func<NewComplexityValidationRule.FieldImpactContext, (double, double)>?>(COMPLEXITY_IMPACT_FUNC);
}

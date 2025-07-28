using GraphQL.Types;
using GraphQL.Validation.Complexity;
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
    /// Specify field's complexity impact which will be taken into account by <see cref="ComplexityValidationRule"/>.
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
        return provider.WithComplexityImpact(context => new(impact, context.Configuration.DefaultComplexityImpactDelegate(context).ChildImpactMultiplier));
    }

    /// <summary>
    /// Specify field's complexity impact which will be taken into account by <see cref="ComplexityValidationRule"/>.
    /// </summary>
    /// <typeparam name="TMetadataProvider">The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining.</typeparam>
    /// <param name="provider">Metadata provider which must implement <see cref="IMetadataWriter"/> interface.</param>
    /// <param name="fieldImpact">Field's complexity impact.</param>
    /// <param name="childImpactMultiplier">Multiplier applied to child fields' complexity impact values.</param>
    /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
    public static TMetadataProvider WithComplexityImpact<TMetadataProvider>(this TMetadataProvider provider, double fieldImpact, double childImpactMultiplier)
        where TMetadataProvider : IFieldMetadataWriter
        => provider.WithComplexityImpact(_ => new FieldComplexityResult(fieldImpact, childImpactMultiplier));

    /// <summary>
    /// Specify field's complexity impact delegate which will be taken into account by <see cref="ComplexityValidationRule"/>.
    /// </summary>
    /// <typeparam name="TMetadataProvider">The type of metadata provider. Generics are used here to let compiler infer the returning type to allow methods chaining.</typeparam>
    /// <param name="provider">Metadata provider which must implement <see cref="IMetadataWriter"/> interface.</param>
    /// <param name="func">A function which calculates the complexity impact of the field.</param>
    /// <returns>The reference to the specified <paramref name="provider"/>.</returns>
    public static TMetadataProvider WithComplexityImpact<TMetadataProvider>(this TMetadataProvider provider, Func<FieldImpactContext, FieldComplexityResult> func)
        where TMetadataProvider : IFieldMetadataWriter
        => provider.WithMetadata(COMPLEXITY_IMPACT_FUNC, func);

    /// <summary>
    /// Get field's complexity impact which will be taken into account by <see cref="ComplexityValidationRule"/>.
    /// </summary>
    public static Func<FieldImpactContext, FieldComplexityResult>? GetComplexityImpactDelegate(this FieldType provider)
        => provider.GetMetadata<Func<FieldImpactContext, FieldComplexityResult>?>(COMPLEXITY_IMPACT_FUNC);

    /// <summary>
    /// Configures the schema to use the specified complexity impact for introspection fields.
    /// Specifically, the __typename, __schema, and __type fields' impact is set to <paramref name="impact"/>, and
    /// all their child nodes impact are multiplied by <paramref name="impact"/>. So if the impact is 0, introspection fields
    /// will not be counted towards the complexity, and if the impact is 0.1, introspection fields will be counted at 10%
    /// of their default complexity.
    /// </summary>
    public static TSchema WithIntrospectionComplexityImpact<TSchema>(this TSchema schema, double impact)
        where TSchema : ISchema
    {
        schema.SchemaMetaFieldType.WithComplexityImpact(impact, impact);
        schema.TypeNameMetaFieldType.WithComplexityImpact(impact, impact);
        schema.TypeMetaFieldType.WithComplexityImpact(impact, impact);
        return schema;
    }
}

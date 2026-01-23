namespace GraphQL;

/// <summary>
/// Registers a hand-written GraphQL graph type for inclusion in the AOT-compiled schema.
/// This attribute can be applied multiple times to register multiple graph types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AotGraphTypeAttribute<TGraphType> : AotSchemaAttribute
    where TGraphType : Types.IGraphType
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically register the CLR type mapping for this graph type.
    /// When <see langword="true"/> (default), the generator will create a mapping from the graph type's
    /// generic type parameter to the graph type itself. Set to <see langword="false"/> to skip automatic
    /// CLR type mapping registration when you want to handle the mapping manually.
    /// <para>Mapping is automatically skipped when any of the following are true:</para>
    /// <list type="bullet">
    /// <item>The graph type does not inherit from <see cref="GraphQL.Types.ComplexGraphType{TSourceType}"/> or <see cref="GraphQL.Types.EnumerationGraphType{TEnum}"/>.</item>
    /// <item>The generic type is <see cref="object"/>.</item>
    /// <item>The CLR type is marked with <see cref="GraphQL.DoNotMapClrTypeAttribute"/>.</item>
    /// <item>The CLR type is not marked with <see cref="InstanceSourceAttribute"/> having a value other than <see cref="InstanceSource.ContextSource"/>.</item>
    /// </list>
    /// </summary>
    public bool AutoRegisterClrMapping { get; set; } = true;
}

namespace GraphQL;

/// <summary>
/// Remaps a base graph type to use a derived implementation when requested by the schema.
/// This allows you to substitute a specialized graph type implementation in place of a base type.
/// For example, use <c>[AotRemapType&lt;IdGraphType, GuidGraphType&gt;]</c> to ensure
/// that whenever <c>IdGraphType</c> is requested, <c>GuidGraphType</c> is used instead.
/// This attribute can be applied multiple times to remap multiple graph types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AotRemapTypeAttribute<TGraphType, TGraphTypeImplementation> : AotSchemaAttribute
    where TGraphType : Types.IGraphType
    where TGraphTypeImplementation : Types.IGraphType
{
}

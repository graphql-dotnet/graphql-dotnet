namespace GraphQL;

/// <summary>
/// Specifies a mapping between a CLR type and its corresponding GraphQL type for AOT schema compilation.
/// This attribute can be applied multiple times to specify multiple type mappings.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AotTypeMappingAttribute<[NotAGraphType] TClrType, TGraphType> : AotSchemaAttribute
    where TGraphType : Types.IGraphType
{
}

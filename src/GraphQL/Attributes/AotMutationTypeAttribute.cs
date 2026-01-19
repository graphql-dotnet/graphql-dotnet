namespace GraphQL;

/// <summary>
/// Configures the mutation root type for the schema. Accepts either a CLR type (for automatic generation)
/// or a graph type (for explicit registration). The generator determines which based on whether the type
/// implements <see cref="Types.IGraphType"/>.
/// This attribute is applied to the schema class that derives from <see cref="Types.AotSchema"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AotMutationTypeAttribute<T> : AotSchemaAttribute
{
}

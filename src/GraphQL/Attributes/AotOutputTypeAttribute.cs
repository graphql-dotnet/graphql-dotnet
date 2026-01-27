namespace GraphQL;

/// <summary>
/// Specifies a CLR type that should be used to generate an <see cref="Types.ObjectGraphType{TSourceType}"/> or <see cref="Types.InterfaceGraphType{TSourceType}"/> during AOT schema compilation.
/// This attribute can be applied multiple times to specify multiple output types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AotOutputTypeAttribute<[NotAGraphType] T> : AotSchemaAttribute
{
    /// <summary>
    /// Gets or sets how the CLR type should be represented as a GraphQL output type.
    /// </summary>
    public OutputTypeKind Kind { get; set; } = OutputTypeKind.Auto;
}

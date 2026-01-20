namespace GraphQL;

/// <summary>
/// Specifies a CLR type that should be used to generate an <see cref="Types.ObjectGraphType{TSourceType}"/> or <see cref="Types.InterfaceGraphType{TSourceType}"/> during AOT schema compilation.
/// This attribute can be applied multiple times to specify multiple output types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AotOutputTypeAttribute<[NotAGraphType] T> : AotSchemaAttribute
{
    /// <summary>
    /// Gets or sets whether the CLR type should be treated as an interface type.
    /// When <see langword="true"/>, generates an <see cref="Types.InterfaceGraphType{TSourceType}"/>.
    /// When <see langword="false"/>, generates an <see cref="Types.ObjectGraphType{TSourceType}"/>.
    /// When <see langword="null"/> (default), the type will be auto-detected based on whether the CLR type is an interface.
    /// </summary>
    public bool? IsInterface { get; set; }
}

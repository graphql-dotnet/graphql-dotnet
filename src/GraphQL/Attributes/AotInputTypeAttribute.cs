namespace GraphQL;

/// <summary>
/// Specifies a CLR type that should be used to generate an <see cref="Types.InputObjectGraphType{TSourceType}"/> during AOT schema compilation.
/// This attribute can be applied multiple times to specify multiple input types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AotInputTypeAttribute<[NotAGraphType] T> : AotSchemaAttribute
{
}

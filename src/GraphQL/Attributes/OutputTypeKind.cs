namespace GraphQL;

/// <summary>
/// Specifies how a CLR type should be represented as a GraphQL output type.
/// </summary>
public enum OutputTypeKind
{
    /// <summary>
    /// Auto-detect based on whether the CLR type is an interface (default).
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Generate an ObjectGraphType regardless of whether the CLR type is an interface.
    /// </summary>
    Object = 1,

    /// <summary>
    /// Generate an InterfaceGraphType regardless of whether the CLR type is an interface.
    /// </summary>
    Interface = 2
}

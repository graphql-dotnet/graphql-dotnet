namespace GraphQL.Types;

/// <summary>
/// Provides a mapping from CLR types to graph types.
/// </summary>
public interface IGraphTypeMapping
{
    /// <summary>
    /// Returns a graph type for a given CLR type, or <see langword="null"/> if no mapping is available.
    /// </summary>
    Type? GetGraphTypeFromClrType(Type clrType, bool isInputType);
}

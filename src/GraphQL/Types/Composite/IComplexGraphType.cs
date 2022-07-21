using GraphQLParser;

namespace GraphQL.Types;

/// <summary>
/// Represents an interface for all complex (that is, having their own properties) input and output graph types.
/// </summary>
public interface IComplexGraphType : IGraphType
{
    /// <summary>
    /// Returns a list of the fields configured for this graph type.
    /// </summary>
    TypeFields Fields { get; }

    /// <summary>
    /// Adds a field to this graph type.
    /// </summary>
    FieldType AddField(FieldType fieldType);

    /// <summary>
    /// Returns <see langword="true"/> when a field matching the specified name is configured for this graph type.
    /// </summary>
    bool HasField(string name);

    /// <summary>
    /// Returns the <see cref="FieldType"/> for the field matching the specified name that
    /// is configured for this graph type, or <see langword="null"/> if none is found.
    /// </summary>
    FieldType? GetField(ROM name);
}

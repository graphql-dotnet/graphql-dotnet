namespace GraphQL.Utilities;

/// <summary>
/// Represents a type or directive to be imported from the linked schema, with an optional alias.
/// </summary>
public sealed class LinkImport
{
    /// <summary>
    /// The name of the type or directive to be imported.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// An optional alias for the imported type or directive.
    /// </summary>
    public string? Alias { get; set; }
}

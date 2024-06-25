using GraphQLParser.Visitors;

namespace GraphQL.Utilities;

/// <summary>
/// Options for printing a GraphQL document.
/// </summary>
public class PrintOptions : SDLPrinterOptions
{
    /// <summary>
    /// Indicates whether to print a description for types, fields, directives, arguments and other schema elements.
    /// </summary>
    public bool IncludeDescriptions { get; set; } = true;

    /// <summary>
    /// Indicates whether to print deprecation reasons for fields and enum values.
    /// </summary>
    public bool IncludeDeprecationReasons { get; set; } = true;

    /// <summary>
    /// Gets or sets the string comparer to use when sorting the AST.
    /// Set this value to <see langword="null"/> to disable sorting.
    /// </summary>
    public StringComparison? StringComparison { get; set; }

    /// <summary>
    /// Indicates whether to print federation types.
    /// Only set to <see langword="false"/> for Apollo Federation v1 support.
    /// </summary>
    public bool IncludeFederationTypes { get; set; } = true;

    /// <summary>
    /// Indicates whether to print type/directive definitions imported from another schema via the @link directive.
    /// </summary>
    public bool IncludeImportedTypes { get; set; } = true;
}

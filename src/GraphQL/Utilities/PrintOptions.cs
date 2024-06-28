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
    /// Indicates whether to print Apollo Federation v1 types.
    /// Do not use this flag with Federation v2 or newer; see <see cref="IncludeImportedDefinitions"/>.
    /// </summary>
    public bool IncludeFederationTypes { get; set; } = true;

    /// <summary>
    /// Indicates whether to print type/directive definitions imported from another schema via the @link directive.
    /// Typically disabled with Federation v2 to exclude imported definitions from the printed schema.
    /// </summary>
    public bool IncludeImportedDefinitions { get; set; } = true;
}

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
    public bool IncludeDescriptions { get; set; }

    /// <summary>
    /// Indicates whether to print a deprecation reason for fields and enum values.
    /// </summary>
    public bool IncludeDeprecationReasons { get; set; }

    /// <summary>
    /// Gets or sets the string comparer to use when sorting the AST.
    /// Set this value to <see langword="null"/> to disable sorting.
    /// </summary>
    public StringComparison? StringComparison { get; set; }
}

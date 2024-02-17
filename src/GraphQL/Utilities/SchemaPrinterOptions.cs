using GraphQL.Introspection;

namespace GraphQL.Utilities;

/// <summary>
/// Options for schema printing when using <see cref="SchemaPrinter.Print"/>.
/// </summary>
public class SchemaPrinterOptions
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
    /// Provides the ability to order the schema elements upon printing.
    /// By default all elements are returned as-is; no sorting is applied.
    /// </summary>
    public ISchemaComparer? Comparer { get; set; }
}

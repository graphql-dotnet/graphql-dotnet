using GraphQL.Introspection;

namespace GraphQL.Utilities
{
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
        /// Indicates whether to use ',' instead of '&amp;' when inheriting a type from multiple interfaces.
        /// </summary>
        public bool OldImplementsSyntax { get; set; }

        /// <summary>
        /// Indicates whether to print descriptions as comments for compatibility with the 2016 GraphQL specification.
        /// </summary>
        public bool PrintDescriptionsAsComments { get; set; } = true;

        /// <summary>
        /// Provides the ability to order the schema elements upon printing.
        /// By default all elements are returned as-is; no sorting is applied.
        /// </summary>
        public ISchemaComparer? Comparer { get; set; }
    }
}

using GraphQL.Introspection;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Options for schema printing when using <see cref="SchemaPrinter2"/>.
    /// </summary>
    public class SchemaPrinterOptions2 : SDLPrinterOptions
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
        /// Indicates whether to print 3 builtin directives: @skip, @include, @deprecated.
        /// </summary>
        public bool IncludeBuiltinDirectives { get; set; }

        /// <summary>
        /// Indicates whether to print 5 builtin scalars: String, Boolean, Int, Float, ID.
        /// </summary>
        public bool IncludeBuiltinScalars { get; set; }

        /// <summary>
        /// Provides the ability to order the schema elements upon printing.
        /// By default all elements are returned as-is; no sorting is applied.
        /// </summary>
        public ISchemaComparer? Comparer { get; set; }
    }
}

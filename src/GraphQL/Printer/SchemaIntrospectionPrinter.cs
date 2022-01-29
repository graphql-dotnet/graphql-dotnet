using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Enables printing introspection schema as SDL (Schema Definition Language) document.
    /// <br/>
    /// See <see href="http://spec.graphql.org/October2021/#sec-Introspection"/> for more information.
    /// </summary>
    public class SchemaIntrospectionPrinter
    {
        private readonly ASTIntrospectionPrinter _astPrinter;

        public SchemaIntrospectionPrinter()
            : this(new SchemaIntrospectionPrinterOptions())
        {
        }

        public SchemaIntrospectionPrinter(SchemaIntrospectionPrinterOptions options)
        {
            Options = options;
            _astPrinter = new ASTIntrospectionPrinter(options);
        }

        /// <summary>
        /// Options used to print introspection schema.
        /// </summary>
        public SchemaIntrospectionPrinterOptions Options { get; }

        /// <summary>
        /// Prints introspection schema in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintAsync(ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var converter = new ASTConverter(new ASTConverterOptions
            {
                IncludeDescriptions = Options.IncludeDescriptions,
                Comparer = Options.Comparer
            });

            await _astPrinter.PrintAsync(converter.Convert(schema), writer, cancellationToken);
        }
    }
}

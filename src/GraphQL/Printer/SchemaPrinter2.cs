using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Enables printing schema as SDL (Schema Definition Language) document.
    /// <br/>
    /// See <see href="http://spec.graphql.org/October2021/#sec-Type-System"/> for more information.
    /// </summary>
    public class SchemaPrinter2
    {
        private readonly ASTPrinter _astPrinter;

        public SchemaPrinter2()
            : this(new SchemaPrinterOptions2())
        {
        }

        public SchemaPrinter2(SchemaPrinterOptions2 options)
        {
            Options = options;
            _astPrinter = new ASTPrinter(options);
        }

        /// <summary>
        /// Options used to print schema.
        /// </summary>
        public SchemaPrinterOptions2 Options { get; }

        /// <summary>
        /// Prints schema in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintAsync(ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var converter = new ASTConverter(new ASTConverterOptions
            {
                IncludeDescriptions = Options.IncludeDescriptions,
                Comparer = Options.Comparer
            });

            var doc = converter.Convert(schema);
            await _astPrinter.PrintAsync(doc, writer, cancellationToken);
        }

        /// <summary>
        /// Prints schema definition in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintSchemaDefinitionAsync(ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var converter = new ASTConverter(new ASTConverterOptions
            {
                IncludeDescriptions = Options.IncludeDescriptions,
                Comparer = Options.Comparer
            });

            var def = converter.ConvertSchemaDefinition(schema);
            if (def != null)
                await _astPrinter.PrintAsync(def, writer, cancellationToken);
        }

        /// <summary>
        /// Prints graph type in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintTypeAsync(IGraphType type, ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var converter = new ASTConverter(new ASTConverterOptions
            {
                IncludeDescriptions = Options.IncludeDescriptions,
                Comparer = Options.Comparer
            });

            var def = converter.ConvertTypeDefinition(type, schema);
            await _astPrinter.PrintAsync(def, writer, cancellationToken);
        }
    }
}

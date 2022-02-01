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

        private ASTConverter CreateConverter() => new ASTConverter(new ASTConverterOptions
        {
            IncludeDescriptions = Options.IncludeDescriptions,
            Comparer = Options.Comparer
        });

        /// <summary>
        /// Prints schema in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintSchemaAsync(ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var doc = CreateConverter().Convert(schema);
            await _astPrinter.PrintAsync(doc, writer, cancellationToken);
        }

        /// <summary>
        /// Prints schema definition in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintSchemaDefinitionAsync(ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var def = CreateConverter().ConvertSchemaDefinition(schema);
            if (def != null)
                await _astPrinter.PrintAsync(def, writer, cancellationToken);
        }

        /// <summary>
        /// Prints graph type in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintTypeAsync(IGraphType type, ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var def = CreateConverter().ConvertTypeDefinition(type, schema);
            await _astPrinter.PrintAsync(def, writer, cancellationToken);
        }

        /// <summary>
        /// Prints directive graph type in the specified <see cref="TextWriter"/>.
        /// </summary>
        public async ValueTask PrintDirectiveAsync(DirectiveGraphType type, ISchema schema, TextWriter writer, CancellationToken cancellationToken = default)
        {
            var def = CreateConverter().ConvertDirectiveDefinition(type, schema);
            await _astPrinter.PrintAsync(def, writer, cancellationToken);
        }
    }
}

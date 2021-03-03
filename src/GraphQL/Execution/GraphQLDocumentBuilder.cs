using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;

namespace GraphQL.Execution
{
    /// <summary>
    /// <inheritdoc cref="IDocumentBuilder"/>
    /// <br/><br/>
    /// Default instance of <see cref="IDocumentBuilder"/>.
    /// </summary>
    public class GraphQLDocumentBuilder : IDocumentBuilder
    {
        private readonly Parser _parser;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public GraphQLDocumentBuilder()
        {
            var lexer = new Lexer();
            _parser = new Parser(lexer);
        }

        /// <inheritdoc/>
        public IParseResult Build(string body)
        {
            var source = new Source(body);
            GraphQLDocument result;
            try
            {
                result = _parser.Parse(source);
            }
            catch (GraphQLSyntaxErrorException ex)
            {
                return new ParseResult(new SyntaxError(ex));
            }

            var document = CoreToVanillaConverter.Convert(body, result);
            document.OriginalQuery = body;
            return new SuccessfullyParsedResult(document);
        }
    }
}

using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQLParser;

namespace GraphQL.Execution
{
    public class GraphQLDocumentBuilder : IDocumentBuilder
    {
        private readonly Lexer _lexer;
        private readonly Parser _parser;
        private readonly CoreToVanillaConverter _converter;

        public GraphQLDocumentBuilder()
        {
            _lexer = new Lexer();
            _parser = new Parser(_lexer);
            _converter = new CoreToVanillaConverter();
        }

        public Document Build(string body)
        {
            var source = new Source(body);
            var result = _parser.Parse(source);
            var document = _converter.Convert(body, result);
            document.OriginalQuery = body;
            return document;
        }
    }
}

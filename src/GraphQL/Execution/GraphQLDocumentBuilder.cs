using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQLParser;

namespace GraphQL.Execution
{
    public class GraphQLDocumentBuilder : IDocumentBuilder
    {
        private readonly Parser _parser;

        public GraphQLDocumentBuilder()
        {
            var lexer = new Lexer();
            _parser = new Parser(lexer);
        }

        public Document Build(string body)
        {
            var source = new Source(body);
            var result = _parser.Parse(source);

            var document = CoreToVanillaConverter.Convert(body, result);
            document.OriginalQuery = body;
            return document;
        }
    }
}

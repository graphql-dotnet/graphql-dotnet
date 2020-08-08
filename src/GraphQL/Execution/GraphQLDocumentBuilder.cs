using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;

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
            GraphQLDocument result;
            try
            {
                result = _parser.Parse(source);
            }
            catch (GraphQLSyntaxErrorException ex)
            {
                var e = new ExecutionError(ex.Description, ex);
                e.AddLocation(ex.Line, ex.Column);
                throw e;
            }

            var document = CoreToVanillaConverter.Convert(body, result);
            document.OriginalQuery = body;
            return document;
        }
    }
}

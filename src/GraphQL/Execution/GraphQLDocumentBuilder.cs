using System;
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
            GraphQLParser.AST.GraphQLDocument result;
            try
            {
                result = _parser.Parse(source);
            }
            catch (GraphQLParser.Exceptions.GraphQLSyntaxErrorException ex)
            {
                ExecutionError rethrowException = null;
                try
                {
                    //e.g. message = "Syntax Error GraphQL (1:1) Unexpected Name \"unknownoperation\"\n1: unknownoperation { firstAsync }\n   ^\n"
                    var message = ex.Message.Substring(0, ex.Message.IndexOf('\n'));
                    var paren = ex.Message.IndexOf('(');
                    var colon = ex.Message.IndexOf(':', paren);
                    var paren2 = ex.Message.IndexOf(')', colon);
                    var line = ex.Message.Substring(paren + 1, colon - paren - 1);
                    var column = ex.Message.Substring(colon + 1, paren2 - colon - 1);
                    var messageDescription = message.Substring(paren2 + 1).Trim();

                    //e.g. newMessageDescription = "Error parsing query: Unexpected Name \"unknownoperation\""
                    var newMessageDescription = "Error parsing query: " + messageDescription;

                    rethrowException = new ExecutionError(newMessageDescription, ex);
                    rethrowException.AddLocation(int.Parse(line), int.Parse(column));
                    //rethrowException.Code will default to "SYNTAX_ERROR"
                }
                catch
                {
                }

                if (rethrowException != null)
                    throw rethrowException;

                throw;
            }

            var document = CoreToVanillaConverter.Convert(body, result);
            document.OriginalQuery = body;
            return document;
        }
    }
}

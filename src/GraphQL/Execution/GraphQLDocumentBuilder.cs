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
        /// <summary>
        /// Specifies whether to ignore comments when parsing GraphQL document.
        /// By default, all comments are ignored
        /// </summary>
        public bool IgnoreComments { get; set; } = true;

        /// <inheritdoc/>
        public IParseResult Build(string body)
        {
            GraphQLDocument result;
            try
            {
                result = Parser.Parse(body, new ParserOptions { Ignore = IgnoreComments ? IgnoreOptions.IgnoreComments : IgnoreOptions.None });
            }
            catch (GraphQLSyntaxErrorException ex)
            {
                return new ParseResult(new SyntaxError(ex));
            }

            var document = CoreToVanillaConverter.Convert(result);
            document.OriginalQuery = body;
            return new SuccessfullyParsedResult(document);
        }
    }
}

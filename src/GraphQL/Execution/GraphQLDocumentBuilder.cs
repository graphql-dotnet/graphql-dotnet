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
        /// By default, all comments are ignored.
        /// </summary>
        public bool IgnoreComments { get; set; } = true;

        /// <summary>
        /// Specifies whether to ignore token locations when parsing GraphQL document.
        /// By default, all token locations are taken into account.
        /// </summary>
        public bool IgnoreLocations { get; set; }

        /// <summary>
        /// Maximum allowed recursion depth during parsing.
        /// Depth is calculated in terms of AST nodes.
        /// <br/>
        /// Defaults to 128 if not set.
        /// Minimum value is 1.
        /// </summary>
        public int? MaxDepth { get; set; }

        /// <inheritdoc/>
        public GraphQLDocument Build(string body)
        {
            try
            {
                return Parser.Parse(body, new ParserOptions { Ignore = CreateIgnoreOptions(), MaxDepth = MaxDepth });
            }
            catch (GraphQLSyntaxErrorException ex)
            {
                throw new SyntaxError(ex);
            }
        }

        private IgnoreOptions CreateIgnoreOptions()
        {
            var options = IgnoreOptions.None;
            if (IgnoreComments)
                options |= IgnoreOptions.Comments;
            if (IgnoreLocations)
                options |= IgnoreOptions.Locations;
            return options;
        }
    }
}

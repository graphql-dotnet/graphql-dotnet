using GraphQL.Language.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Creates a Document representing a GraphQL query from a GraphQL request string
    /// </summary>
    public interface IDocumentBuilder
    {
        /// <summary>
        /// Parse a GraphQL request and return a Document representing the request
        /// </summary>
        Document Build(string body);
    }
}

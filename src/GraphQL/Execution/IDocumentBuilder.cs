using GraphQL.Language.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Creates a <see cref="Document">Document</see> representing a GraphQL query from a GraphQL request string
    /// </summary>
    public interface IDocumentBuilder
    {
        /// <summary>
        /// Parse a GraphQL request and return a <see cref="Document">Document</see> representing the request
        /// </summary>
        Document Build(string body);
    }
}

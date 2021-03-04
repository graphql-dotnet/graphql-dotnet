using GraphQL.Language.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Creates a <see cref="Document">Document</see> representing a GraphQL AST from a plain GraphQL query string
    /// </summary>
    public interface IDocumentBuilder
    {
        /// <summary>
        /// Parse a GraphQL request and return a <see cref="Document">Document</see> representing the GraphQL request AST
        /// </summary>
        Document Build(string body);
    }
}
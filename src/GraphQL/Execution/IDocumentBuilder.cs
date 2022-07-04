using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Creates a <see cref="GraphQLDocument">Document</see> representing a GraphQL AST from a plain GraphQL query string
    /// </summary>
    public interface IDocumentBuilder
    {
        /// <summary>
        /// Parse a GraphQL request and return a <see cref="GraphQLDocument">Document</see> representing the GraphQL request AST
        /// </summary>
        GraphQLDocument Build(string body);
    }
}

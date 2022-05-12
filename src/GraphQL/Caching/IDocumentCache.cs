using GraphQLParser.AST;

namespace GraphQL.Caching
{
    /// <summary>
    /// Represents a cache of validated AST documents based on a query.
    /// </summary>
    public interface IDocumentCache : ICache<GraphQLDocument>
    {
    }
}

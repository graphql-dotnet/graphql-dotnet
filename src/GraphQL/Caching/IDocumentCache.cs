using GraphQL.Language.AST;

namespace GraphQL.Caching
{
    /// <summary>
    /// Represents a cache of validated AST documents based on a query.
    /// </summary>
    public interface IDocumentCache
    {
        /// <summary>
        /// Gets or sets a document in the cache. Must be thread-safe. Returns <see langword="null"/> if no entry is found.
        /// </summary>
        Document? this[string query] { get; set; }
    }
}

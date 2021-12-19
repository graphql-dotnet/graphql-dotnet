using System.Threading.Tasks;
using GraphQL.Language.AST;

namespace GraphQL.Caching
{
    /// <summary>
    /// Represents a cache of validated AST documents based on a query.
    /// </summary>
    public interface IDocumentCache
    {
        /// <summary>
        /// Gets a document in the cache. Must be thread-safe. Returns <see langword="null"/> if no entry is found.
        /// </summary>
        ValueTask<Document?> GetAsync(string query);

        /// <summary>
        /// Sets a document in the cache. Must be thread-safe.
        /// </summary>
        ValueTask SetAsync(string query, Document value);

        /// <summary>
        /// Remove a document cache. Must be thread-safe.
        /// </summary>
        ValueTask RemoveAsync(string query);
    }
}

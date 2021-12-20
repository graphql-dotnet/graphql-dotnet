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
        /// Gets a document in the cache. Must be thread-safe.
        /// </summary>
        /// <param name="query">As the cache key</param>
        /// <returns>The cached document object. Returns <see langword="null"/> if no entry is found.</returns>
        ValueTask<Document?> GetAsync(string query);

        /// <summary>
        /// Sets a document in the cache. Must be thread-safe.
        /// </summary>
        /// <param name="query">As the cache key</param>
        /// <param name="value">The document object to cache. The existing cache item will be removed if the value is <see langword="null"/>.</param>
        ValueTask SetAsync(string query, Document? value);
    }
}

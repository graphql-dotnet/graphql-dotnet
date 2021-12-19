using System.Threading.Tasks;
using GraphQL.Language.AST;

namespace GraphQL.Caching
{
    /// <summary>
    /// The default implementation of <see cref="IDocumentCache"/> which
    /// does not provide caching services.
    /// </summary>
    public sealed class DefaultDocumentCache : IDocumentCache
    {
        private DefaultDocumentCache() { }

        /// <summary>
        /// Provides a static instance of this class.
        /// </summary>
        public static readonly DefaultDocumentCache Instance = new DefaultDocumentCache();

        public Task<Document?> GetAsync(string query) => Task.FromResult<Document?>(null);

        public Task SetAsync(string query, Document? value) => Task.CompletedTask;
    }
}

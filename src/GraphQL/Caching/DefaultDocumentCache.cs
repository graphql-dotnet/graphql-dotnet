using GraphQLParser.AST;

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

        /// <inheritdoc/>
        public ValueTask<GraphQLDocument?> GetAsync(string query) => default;

        /// <inheritdoc/>
        public ValueTask SetAsync(string query, GraphQLDocument value) => default;
    }
}

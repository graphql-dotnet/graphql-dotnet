using GraphQL.DI;
using GraphQL.PersistedDocuments;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching;

/// <summary>
/// A basic implementation of a document cache, limited by a configured amount of memory.
/// </summary>
public class MemoryDocumentCache : IConfigureExecution, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryDocumentCacheOptions _options;
    private readonly bool _memoryCacheIsOwned;

    /// <summary>
    /// Initializes a new instance with the default options: 100,000 maximum total query size and no expiration time.
    /// Anticipate memory use of approximately 1MB.
    /// </summary>
    public MemoryDocumentCache()
        : this(new MemoryDocumentCacheOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified options. Set the <see cref="MemoryCacheOptions.SizeLimit"/>
    /// value to the maximum total query size to be cached. Anticipate about 10x maximum memory use above that value.
    /// </summary>
    /// <param name="options">A value containing the <see cref="MemoryDocumentCacheOptions"/> to use.</param>
    public MemoryDocumentCache(IOptions<MemoryDocumentCacheOptions> options)
        : this(
            new MemoryCache(options),
            true,
            options)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified memory cache and options.
    /// Note that by overriding <see cref="GetMemoryCacheEntryOptions(ExecutionOptions, GraphQLDocument)"/>,
    /// the sliding expiration time specified within <paramref name="options"/> can be ignored.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance to use.</param>
    /// <param name="disposeMemoryCache">Indicates if the memory cache is disposed when this instance is disposed.</param>
    /// <param name="options">Provides option values for use by <see cref="GetMemoryCacheEntryOptions(ExecutionOptions, GraphQLDocument)"/>; optional.</param>
    protected MemoryDocumentCache(IMemoryCache memoryCache, bool disposeMemoryCache, IOptions<MemoryDocumentCacheOptions> options)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _memoryCacheIsOwned = disposeMemoryCache;
    }

    /// <summary>
    /// Returns a <see cref="MemoryCacheEntryOptions"/> instance for the specified document.
    /// Defaults to setting the <see cref="MemoryCacheEntryOptions.SlidingExpiration"/> value as specified
    /// in options, and the <see cref="MemoryCacheEntryOptions.Size"/> value to the length of the document's source.
    /// </summary>
    protected virtual MemoryCacheEntryOptions GetMemoryCacheEntryOptions(ExecutionOptions options, GraphQLDocument document)
    {
        // If the ExecutionOptions contains a query, use the obsolete method.
        if (options.Query != null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return GetMemoryCacheEntryOptions(options);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // document.Source is a struct and will never be null
        // if it is not initialized, it will have a length of 0 (however, this should not occur with vanilla GraphQL code)
        // so we set it to 100 if it is 0, to avoid a cache entry with a size of 0
        var docLength = document.Source.Length;
        if (docLength == 0)
            docLength = 100;
        return new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration, Size = docLength };
    }

    /// <summary>
    /// Returns a <see cref="MemoryCacheEntryOptions"/> instance for the specified query.
    /// Defaults to setting the <see cref="MemoryCacheEntryOptions.SlidingExpiration"/> value as specified
    /// in options, and the <see cref="MemoryCacheEntryOptions.Size"/> value to the length of the query.
    /// </summary>
    [Obsolete("This method is obsolete and will be removed in a future version. Use GetMemoryCacheEntryOptions(ExecutionOptions, GraphQLDocument) instead.")]
    protected virtual MemoryCacheEntryOptions GetMemoryCacheEntryOptions(ExecutionOptions options)
    {
        return new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration, Size = options.Query!.Length };
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        if (_memoryCacheIsOwned)
            _memoryCache.Dispose();
    }

    /// <summary>
    /// Gets the cache key for the current <see cref="ExecutionOptions"/>.
    /// The key is a <see cref="CacheItem"/> record that always contains the query (or document id)
    /// plus an extra property computed via the delegate supplied in options.
    /// </summary>
    private CacheItem GetCacheKey(ExecutionOptions options)
    {
        // Compute the extra value via the delegate. By default, this returns the schema.
        var selector = _options.AdditionalCacheKeySelector
            ?? DefaultSelector;
        var extra = selector.Invoke(options);
        return new CacheItem(options, extra);

        static object? DefaultSelector(ExecutionOptions options)
        {
            // pull the schema; omit the selector if the schema is not present
            var schema = options.Schema;
            if (schema == null)
                return null;

            // for schema-first or manually created schemas, return the schema instance
            var schemaType = schema.GetType();
            if (schemaType == typeof(Schema))
                return schema;

            // for code-first and type-first schemas, return the schema type, thus supporting scoped schemas
            return schemaType;
        }
    }

    /// <summary>
    /// Gets a document in the cache. Must be thread-safe.
    /// </summary>
    /// <param name="options"><see cref="ExecutionOptions"/></param>
    /// <returns>The cached document object. Returns <see langword="null"/> if no entry is found.</returns>
    protected virtual ValueTask<GraphQLDocument?> GetAsync(ExecutionOptions options) =>
        new(_memoryCache.TryGetValue<GraphQLDocument>(GetCacheKey(options), out var value) ? value : null);

    /// <summary>
    /// Sets a document in the cache. Must be thread-safe.
    /// </summary>
    /// <param name="options"><see cref="ExecutionOptions"/></param>
    /// <param name="value">The document object to cache.</param>
    protected virtual ValueTask SetAsync(ExecutionOptions options, GraphQLDocument value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _memoryCache.Set(GetCacheKey(options), value, GetMemoryCacheEntryOptions(options, value));

        return default;
    }

    /// <summary>
    /// Creates an <see cref="ExecutionResult"/> containing the provided error.
    /// Override this method to change the error response.
    /// </summary>
    protected virtual ExecutionResult CreateExecutionResult(ExecutionError error) => new(error);

    /// <inheritdoc />
    public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        if (options.Document == null && (options.Query != null || options.DocumentId != null))
        {
            // Ensure that both documentId and query are not provided.
            if (options.DocumentId != null && options.Query != null)
                return CreateExecutionResult(new InvalidRequestError());

            var document = await GetAsync(options).ConfigureAwait(false);
            if (document != null) // Cache hit.
            {
                options.Document = document;
                // None of the default validation rules are dependent on the inputs; thus,
                // a successfully cached document should not need additional validation.
                options.ValidationRules = options.CachedDocumentValidationRules ?? [];
            }

            var result = await next(options).ConfigureAwait(false);

            if (result.Executed && // Validation succeeded.
                document == null && // Cache miss.
                result.Document != null)
            {
                // Note: At this point, the persisted document handler may have set Query.
                // In that case, both Query and DocumentId are set and caching is based on DocumentId only.
                await SetAsync(options, result.Document).ConfigureAwait(false);
            }

            return result;
        }
        else
        {
            return await next(options).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public virtual float SortOrder => 200;

    // Private cache key type that includes the query (or document id) plus an extra value.
    private record class CacheItem
    {
        public string? Query { get; }
        public string? DocumentId { get; }
        public object? Extra { get; }

        public CacheItem(ExecutionOptions options, object? extra)
        {
            // Cache based on the document id if present, or the query if not.
            Query = options.DocumentId != null ? null : options.Query;
            DocumentId = options.DocumentId;
            Extra = extra;
        }
    }
}

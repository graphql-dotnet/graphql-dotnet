using GraphQL.DI;
using GraphQL.PersistedDocuments;
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
    /// Note that by overriding <see cref="GetMemoryCacheEntryOptions(ExecutionOptions)"/>, the sliding expiration
    /// time specified within <paramref name="options"/> can be ignored.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance to use.</param>
    /// <param name="disposeMemoryCache">Indicates if the memory cache is disposed when this instance is disposed.</param>
    /// <param name="options">Provides option values for use by <see cref="GetMemoryCacheEntryOptions(ExecutionOptions)"/>; optional.</param>
    protected MemoryDocumentCache(IMemoryCache memoryCache, bool disposeMemoryCache, IOptions<MemoryDocumentCacheOptions> options)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _memoryCacheIsOwned = disposeMemoryCache;
    }

    /// <summary>
    /// Returns a <see cref="MemoryCacheEntryOptions"/> instance for the specified query.
    /// Defaults to setting the <see cref="MemoryCacheEntryOptions.SlidingExpiration"/> value as specified
    /// in options, and the <see cref="MemoryCacheEntryOptions.Size"/> value to the length of the query.
    /// </summary>
    protected virtual MemoryCacheEntryOptions GetMemoryCacheEntryOptions(ExecutionOptions options, GraphQLDocument document)
    {
        // use the length of the Query property if it is present
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
    /// Gets a document in the cache. Must be thread-safe.
    /// </summary>
    /// <param name="options"><see cref="ExecutionOptions"/></param>
    /// <returns>The cached document object. Returns <see langword="null"/> if no entry is found.</returns>
    protected virtual ValueTask<GraphQLDocument?> GetAsync(ExecutionOptions options) =>
        new(_memoryCache.TryGetValue<GraphQLDocument>(new CacheItem(options), out var value) ? value : null);

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

        _memoryCache.Set(new CacheItem(options), value, GetMemoryCacheEntryOptions(options, value));

        return default;
    }

    /// <summary>
    /// Create <see cref="ExecutionResult"/> with provided error.
    /// Override this method to change the provided error responses.
    /// </summary>
    protected virtual ExecutionResult CreateExecutionResult(ExecutionError error) => new(error);

    /// <inheritdoc />
    public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        if (options.Document == null && (options.Query != null || options.DocumentId != null))
        {
            // ensure that both documentId and query are not provided
            if (options.DocumentId != null && options.Query != null)
                return CreateExecutionResult(new InvalidRequestError());

            var document = await GetAsync(options).ConfigureAwait(false);
            if (document != null) // already in cache
            {
                options.Document = document;
                // none of the default validation rules yet are dependent on the inputs, and the
                // operation name is not passed to the document validator, so any successfully cached
                // document should not need any validation rules run on it
                options.ValidationRules = options.CachedDocumentValidationRules ?? Array.Empty<IValidationRule>();
            }

            var result = await next(options).ConfigureAwait(false);

            if (result.Executed && // that is, validation was successful
                document == null && // cache miss
                result.Document != null)
            {
                // note: at this point, the persisted document handler may have set Query, in which case both Query and
                //   DocumentId will be set, and we will cache based on the documentId only
                // but if only Query was initially provided, it will still be the only property set
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

    private record class CacheItem
    {
        public string? Query { get; }
        public string? DocumentId { get; }

        public CacheItem(ExecutionOptions options)
        {
            // cache based on the document id if present, or the query if not, but not both
            Query = options.DocumentId != null ? null : options.Query;
            DocumentId = options.DocumentId;
        }
    }
}

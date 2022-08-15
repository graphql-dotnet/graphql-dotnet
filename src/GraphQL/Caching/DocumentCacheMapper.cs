using GraphQL.DI;
using GraphQL.Validation;

namespace GraphQL.Caching;

/// <summary>
/// Maps the registered <see cref="IDocumentCache"/> instance as an <see cref="IConfigureExecution"/> instance.
/// </summary>
[Obsolete("Remove in v8")]
internal class DocumentCacheMapper : IConfigureExecution
{
    internal IDocumentCache DocumentCache { get; }

    public DocumentCacheMapper(IDocumentCache documentCache)
    {
        DocumentCache = documentCache;
    }

    /// <inheritdoc />
    public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        if (options.Document == null && options.Query != null)
        {
            var document = await DocumentCache.GetAsync(options.Query).ConfigureAwait(false);
            if (document != null) // already in cache
                // none of the default validation rules yet are dependent on the inputs, and the
                // operation name is not passed to the document validator, so any successfully cached
                // document should not need any validation rules run on it
                options.ValidationRules = options.CachedDocumentValidationRules ?? Array.Empty<IValidationRule>();

            var result = await next(options).ConfigureAwait(false);

            if (result.Executed && // that is, validation was successful
                document == null && // cache miss
                options.Document != null)
                await DocumentCache.SetAsync(options.Query, options.Document).ConfigureAwait(false);

            return result;
        }
        else
        {
            return await next(options).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public virtual float SortOrder => 200;
}

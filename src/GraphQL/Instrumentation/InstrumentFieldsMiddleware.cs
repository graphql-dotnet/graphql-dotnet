using GraphQL.DataLoader;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Middleware required for Apollo tracing to record performance metrics of field resolvers.
    /// </summary>
    public class InstrumentFieldsMiddleware : IFieldMiddleware
    {
        /// <inheritdoc/>
        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            return context.Metrics.Enabled
                ? ResolveWhenMetricsEnabledAsync(context, next)
                : next(context);
        }

        private async ValueTask<object?> ResolveWhenMetricsEnabledAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var name = context.FieldAst.Name.StringValue; //ISSUE:allocation

            var metadata = new Dictionary<string, object?>
            {
                { "typeName", context.ParentType.Name },
                { "fieldName", name },
                { "returnTypeName", context.FieldDefinition.ResolvedType!.ToString() },
                { "path", context.ResponsePath },
            };

            var marker = context.Metrics.Subject("field", name, metadata);
            var disposeMarker = true;
            try
            {
                var ret = await next(context).ConfigureAwait(false);
                if (ret is IDataLoaderResult dataLoaderResult)
                {
                    disposeMarker = false;
                    return new CompleteDataLoaderResult(dataLoaderResult, marker);
                }
                return ret;
            }
            finally
            {
                if (disposeMarker)
                    marker.Dispose();
            }
        }

        private class CompleteDataLoaderResult : IDataLoaderResult
        {
            private readonly IDataLoaderResult _baseDataLoaderResult;
            private readonly Metrics.Marker _marker;

            public CompleteDataLoaderResult(IDataLoaderResult baseDataLoaderResult, Metrics.Marker marker)
            {
                _baseDataLoaderResult = baseDataLoaderResult;
                _marker = marker;
            }

            public async Task<object?> GetResultAsync(CancellationToken cancellationToken = default)
            {
                using (_marker)
                    return await _baseDataLoaderResult.GetResultAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

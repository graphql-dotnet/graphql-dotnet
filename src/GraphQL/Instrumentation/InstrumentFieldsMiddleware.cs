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
                { "path", context.Path },
            };

            using (context.Metrics.Subject("field", name, metadata))
                return await next(context).ConfigureAwait(false);
        }
    }
}

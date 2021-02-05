using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Utilities;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Middleware required for Apollo tracing to record performance metrics of field resolvers.
    /// </summary>
    public class InstrumentFieldsMiddleware : IFieldMiddleware
    {
        /// <inheritdoc/>
        public async Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var metadata = new Dictionary<string, object>
            {
                { "typeName", context.ParentType.Name },
                { "fieldName", context.FieldName },
                { "returnTypeName", SchemaPrinter.ResolveName(context.ReturnType) },
                { "path", context.Path },
            };

            using (context.Metrics.Subject("field", context.FieldName, metadata))
                return await next(context).ConfigureAwait(false);
        }
    }
}

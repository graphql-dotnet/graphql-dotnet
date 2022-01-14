using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Middleware required for Apollo tracing to record performance metrics of field resolvers.
    /// </summary>
    public class InstrumentFieldsMiddleware : IFieldMiddleware
    {
        /// <inheritdoc/>
        public async Task<object?> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var metadata = new Dictionary<string, object?>
            {
                { "typeName", context.ParentType.Name },
                { "fieldName", context.FieldAst.Name },
                { "returnTypeName", context.FieldDefinition.ResolvedType!.ToString() },
                { "path", context.Path },
            };

            using (context.Metrics.Subject("field", (string)context.FieldAst.Name, metadata)) //TODO:!!!alloc
                return await next(context).ConfigureAwait(false);
        }
    }
}

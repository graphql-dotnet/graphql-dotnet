using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Instrumentation;

namespace GraphQL.Federation.Instrumentation
{
    /// <summary>
    /// Middleware required for Apollo federated tracing to record performance metrics of field.
    /// </summary>
    public class FederatedInstrumentFieldMiddleware : IFieldMiddleware
    {
        /// <inheritdoc/>
        public async Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var metadata = new Dictionary<string, object>
            {
                { "responseName", context.FieldDefinition.Name},
                { "type", context.FieldDefinition.ResolvedType.ToString() },
                { "parentType", context.ParentType.Name },                
                { "path", context.Path },
                { "errors", context.Errors},
            };
           
            using (context.Metrics.Subject("federatedfield", context.FieldAst.Name, metadata))
            {
                object result = await next(context).ConfigureAwait(false);
                return result;
            }
        }
    }
}

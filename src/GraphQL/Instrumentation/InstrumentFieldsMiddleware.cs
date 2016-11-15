using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public class InstrumentFieldsMiddleware
    {
        public Task<object> Resolve(ResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var metadata = new Dictionary<string, object>
            {
                {"typeName", context.ParentType.Name},
                {"fieldName", context.FieldName}
            };

            using (context.Metrics.Subject("field", context.FieldName, metadata))
            {
                return next(context);
            }
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Instrumentation
{
    public class InstrumentFieldsMiddleware
    {
        public async Task<object> Resolve(ResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            var metadata = new Dictionary<string, object>
            {
                {"typeName", context.ParentType.Name},
                {"fieldName", context.FieldName},
                {"returnTypeName", SchemaPrinter.ResolveName(context.ReturnType)},
                {"path", context.Path},
            };

            using (context.Metrics.Subject("field", context.FieldName, metadata))
            {
                var result = await next(context);
                return result;
            }
        }
    }
}

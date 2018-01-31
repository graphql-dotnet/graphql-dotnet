using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public class SerialExecutionStrategy : ExecutionStrategy
    {
        protected override async Task<IDictionary<string, object>> ExecuteFieldsAsync(ExecutionContext context, IObjectGraphType rootType, object source, Dictionary<string, Field> fields, IEnumerable<string> path)
        {
            var data = new Dictionary<string, object>();

            foreach (var fieldCollection in fields)
            {
                var currentPath = path.Concat(new[] { fieldCollection.Key });

                var field = fieldCollection.Value;
                var fieldType = GetFieldDefinition(context.Document, context.Schema, rootType, field);

                // Process each field serially
                await ExtractFieldAsync(context, rootType, source, field, fieldType, data, currentPath)
                    .ConfigureAwait(false);
            }

            foreach (var listener in context.Listeners)
            {
                await listener.BeforeResolveLevelAwaitedAsync(context.UserContext, context.CancellationToken)
                    .ConfigureAwait(false);
            }

            return data;
        }
    }
}

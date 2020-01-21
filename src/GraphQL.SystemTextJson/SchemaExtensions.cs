using System;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.SystemTextJson
{
    public static class SchemaExtensions
    {
        public static Task<string> ExecuteAsync(this ISchema schema, Action<ExecutionOptions> configure)
        {
            var documentWriter = new DocumentWriter(new JsonSerializerOptions() { WriteIndented = true });
            return schema.ExecuteAsync(documentWriter, configure);
        }
    }
}

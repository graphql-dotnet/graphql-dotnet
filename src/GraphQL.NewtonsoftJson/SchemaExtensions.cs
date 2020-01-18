using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.NewtonsoftJson
{
    public static class SchemaExtensions
    {
        public static Task<string> ExecuteAsync(this ISchema schema, Action<ExecutionOptions> configure)
        {
            return schema.ExecuteAsync(new DocumentWriter(indent: true), configure);
        }
    }
}

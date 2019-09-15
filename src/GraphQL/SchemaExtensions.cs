using System;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Types;

namespace GraphQL
{
    public static class SchemaExtensions
    {
        public static string Execute(this ISchema schema, Action<ExecutionOptions> configure)
        {
            return ExecuteAsync(schema, configure).GetAwaiter().GetResult();
        }

        public static async Task<string> ExecuteAsync(this ISchema schema, Action<ExecutionOptions> configure)
        {
            var executor = new DocumentExecuter();
            var result = await executor.ExecuteAsync(options =>
            {
                options.Schema = schema;
                configure(options);
            }).ConfigureAwait(false);

            return await new DocumentWriter(indent: true).WriteToStringAsync(result).ConfigureAwait(false);
        }
    }
}

using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL
{
    public static class SchemaExtensions
    {
        public static async Task<string> ExecuteAsync(this ISchema schema, IDocumentWriter documentWriter, Action<ExecutionOptions> configure)
        {
            var executor = new DocumentExecuter();
            var result = await executor.ExecuteAsync(options =>
            {
                options.Schema = schema;
                configure(options);
            }).ConfigureAwait(false);

            return await documentWriter.WriteToStringAsync(result).ConfigureAwait(false);
        }
    }
}

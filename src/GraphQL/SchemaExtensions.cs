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
            var executionOptions = new ExecutionOptions();
            configure(executionOptions);

            var executor = new DocumentExecuter();
            var result = executor.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                configure(_);
            }).GetAwaiter().GetResult();
            return new DocumentWriter(indent: executionOptions.Indent).Write(result);
        }

        public static async Task<string> ExecuteAsync(this ISchema schema, Action<ExecutionOptions> configure)
        {
            var executionOptions = new ExecutionOptions();
            configure(executionOptions);

            var executor = new DocumentExecuter();
            var result = await executor.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                configure(_);
            });
            return new DocumentWriter(indent: executionOptions.Indent).Write(result);
        }
    }
}

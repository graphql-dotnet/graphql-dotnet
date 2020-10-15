using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL
{
    public static class SchemaExtensions
    {
        /// <summary>
        /// Executes a GraphQL request with the default <see cref="DocumentExecuter"/>, serializes the result using the specified <see cref="IDocumentWriter"/>, and returns the result
        /// </summary>
        /// <param name="schema">An instance of <see cref="ISchema"/> to use to execute the query</param>
        /// <param name="documentWriter">An instance of <see cref="IDocumentExecuter"/> to use to serialize the result</param>
        /// <param name="configure">A delegate which configures the execution options</param>
        public static async Task<string> ExecuteAsync(this ISchema schema, IDocumentWriter documentWriter, Action<ExecutionOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

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

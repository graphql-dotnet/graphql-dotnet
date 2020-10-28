using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.SystemTextJson
{
    public static class SchemaExtensions
    {
        /// <summary>
        /// Configures an <see cref="ExecutionOptions"/> using the given <paramref name="configure"/> action
        /// then executes those options using the <paramref name="schema"/> and a <see cref="DocumentWriter"/>
        /// with indentation turned on. 
        /// </summary>
        /// <param name="schema">A schema to use.</param>
        /// <param name="configure">An action that configures something to execute.</param>
        /// <returns>The JSON result as a string.</returns>
        /// <remarks>
        /// Useful for quickly executing something and "getting started".
        /// Part of the public API and should not be removed even if it has no references.
        /// </remarks>
        public static Task<string> ExecuteAsync(this ISchema schema, Action<ExecutionOptions> configure)
            => schema.ExecuteAsync(new DocumentWriter(indent: true), configure);
    }
}

using GraphQL.SystemTextJson;

namespace GraphQL
{
    public static class JsonStringExtensions
    {
        /// <summary>
        /// Creates an <see cref="ExecutionResult"/> with it's <see cref="ExecutionResult.Data" />
        /// property set to the strongly-typed representation of <paramref name="json"/>.
        /// </summary>
        /// <param name="json">A json representation of the <see cref="ExecutionResult.Data"/> to be set.</param>
        /// <param name="errors">Any errors.</param>
        /// <param name="executed">Indicates if the operation included execution.</param>
        /// <returns>ExecutionResult.</returns>
        public static ExecutionResult ToExecutionResult(this string json, ExecutionErrors errors = null, bool executed = true)
            => new ExecutionResult
            {
                Data = string.IsNullOrWhiteSpace(json) ? null : json.ToDictionary(),
                Errors = errors,
                Executed = executed
            };
    }
}

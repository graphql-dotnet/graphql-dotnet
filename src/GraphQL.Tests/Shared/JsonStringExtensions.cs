#nullable enable

using System.Collections.Generic;
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
        public static ExecutionResult ToExecutionResult(this string? json, ExecutionErrors? errors = null, bool executed = true)
            => new ExecutionResult
            {
                Data = string.IsNullOrWhiteSpace(json) ? null : json.ToDictionary(),
                Errors = errors,
                Executed = executed
            };

        public static Dictionary<string, object?>? ToDictionary(this string? json)
        {
            if (json == null)
                return null;

            var ret = json.ToInputs();
            if (ret == null)
                return null;

            return new Dictionary<string, object?>(ret);
        }

        private static readonly IGraphQLTextSerializer _serializer = new GraphQLSerializer();

        public static Inputs? ToInputs(this string? json)
            => _serializer.Read<Inputs>(json) ?? Inputs.Empty;
    }
}

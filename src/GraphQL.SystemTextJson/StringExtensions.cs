using System.Collections.Generic;
using System.Text.Json;

namespace GraphQL.SystemTextJson
{
    public static class StringExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ObjectDictionaryConverter()
            }
        };

        /// <summary>
        /// Converts a JSON-formatted string into a dictionary.
        /// </summary>
        /// <param name="json">A JSON formatted string.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this string json)
        {
            var dictionary = json?.ToDictionary();
            return dictionary == null ? new Inputs() : dictionary.ToInputs();
        }

        /// <summary>
        /// Converts a JSON-formatted string into a dictionary of objects of their actual type.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>Dictionary.</returns>
        public static Dictionary<string, object> ToDictionary(this string json)
            => JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);

        /// <summary>
        /// Creates an <see cref="ExecutionResult"/> with it's <see cref="ExecutionResult.Data" />
        /// property set to the strongly-typed representation of <paramref name="json"/>.
        /// </summary>
        /// <param name="json">A json representation of the <see cref="ExecutionResult.Data"/> to be set.</param>
        /// <param name="errors">Any errors.</param>
        /// <returns>ExecutionResult.</returns>
        public static ExecutionResult ToExecutionResult(this string json, ExecutionErrors errors = null)
            => new ExecutionResult { Data = string.IsNullOrWhiteSpace(json) ? null : json.ToDictionary(), Errors = errors };
    }
}

using System.Collections.Generic;
using System.Text.Json;

namespace GraphQL.SystemTextJson
{
    public static class StringExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
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
            => json?.ToDictionary().ToInputs();

        /// <summary>
        /// Converts a dictionary into an <see cref="Inputs"/>.
        /// </summary>
        /// <param name="json">A dictionary.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this Dictionary<string, object> dictionary)
            => dictionary == null ? new Inputs() : new Inputs(dictionary);

        /// <summary>
        /// Converts a JSON formatted string into a dictionary of objects of their actual type.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>Dictionary.</returns>
        public static Dictionary<string, object> ToDictionary(this string json)
            => JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);

        /// <summary>
        /// Converts a JSON formatted string into a dictionary of objects of their actual type.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>Dictionary.</returns>
        public static ExecutionResult ToExecutionResult(this string json, ExecutionErrors errors = null)
        {
            object expected = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                expected = json.ToDictionary();
            }
            return new ExecutionResult { Data = expected, Errors = errors };
        }
    }
}

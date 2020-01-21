using System.Text.Json;

namespace GraphQL.SystemTextJson
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a JSON-formatted string into a dictionary.
        /// </summary>
        /// <param name="json">A JSON formatted string.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this string json)
            => json == null ? null : JsonSerializer.Deserialize<Inputs>(json);
    }
}

using System.Collections.Generic;
using System.Text.Json;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Provides extension methods to deserialize json strings into object dictionaries.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ObjectDictionaryConverter(),
                new JsonConverterBigInteger(),
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
            return dictionary.ToInputs();
        }

        /// <summary>
        /// Converts a JSON-formatted string into a dictionary of objects of their actual type.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>Dictionary.</returns>
        public static Dictionary<string, object> ToDictionary(this string json)
            => JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
    }
}

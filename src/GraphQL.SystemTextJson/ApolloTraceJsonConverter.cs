using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Instrumentation;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Converts an instance of <see cref="ApolloTrace"/> to/from JSON.
    /// </summary>
    public class ApolloTraceJsonConverter : JsonConverter<ApolloTrace>
    {
        private readonly JsonSerializerOptions _optionsNoIndent = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly JsonSerializerOptions _optionsIndent = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public ApolloTraceJsonConverter()
        {
            var converter = new ROMJsonConverter();
            _optionsNoIndent.Converters.Add(converter);
            _optionsIndent.Converters.Add(converter);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ApolloTrace value, JsonSerializerOptions options) //TODO: options ignored
            => JsonSerializer.Serialize(writer, value, options.WriteIndented ? _optionsIndent : _optionsNoIndent);

        /// <inheritdoc/>
        public override ApolloTrace Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<ApolloTrace>(ref reader);
    }
}

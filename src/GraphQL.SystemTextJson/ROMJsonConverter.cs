using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQLParser;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="ROM"/> object.
    /// </summary>
    public class ROMJsonConverter : JsonConverter<ROM>
    {
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ROM value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        /// <inheritdoc/>
        public override ROM Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.String
                ? (ROM)reader.GetString()
                : throw new JsonException();
        }
    }
}

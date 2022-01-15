using System;
using GraphQLParser;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="ROM"/> object.
    /// </summary>
    public class ROMJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(ROM);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((string)(ROM)value);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.TokenType == JsonToken.String
                ? (ROM)(string)reader.Value
                : throw new JsonException();
        }
    }
}

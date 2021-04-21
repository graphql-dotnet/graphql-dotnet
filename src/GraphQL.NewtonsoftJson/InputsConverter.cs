using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// A custom JsonConverter for reading an <see cref="Inputs"/> object.
    /// Doesn't support write.
    /// </summary>
    public class InputsConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => ReadDictionary(reader).ToInputs();

        internal static Dictionary<string, object> ReadDictionary(JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException();

            var result = new Dictionary<string, object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    throw new JsonException();

                string key = (string)reader.Value;

                // move to property value
                if (!reader.Read())
                    throw new JsonException();

                result.Add(key, ReadValue(reader));
            }

            return result;
        }

        private static object ReadValue(JsonReader reader)
            => reader.TokenType switch
            {
                JsonToken.StartArray => ReadArray(reader),
                JsonToken.StartObject => ReadDictionary(reader),
                JsonToken.Integer => ReadNumber(reader),
                JsonToken.Float => reader.Value,
                JsonToken.Boolean => reader.Value,
                JsonToken.String => reader.Value,
                JsonToken.Null => null,
                JsonToken.None => null,
                _ => throw new InvalidOperationException($"Unexpected token type: {reader.TokenType}")
            };

        private static List<object> ReadArray(JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonException();

            var result = new List<object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                result.Add(ReadValue(reader));
            }

            return result;
        }

        private static object ReadNumber(JsonReader reader)
        {
            var value = reader.Value;
            if (value is long l && l >= int.MinValue && l <= int.MaxValue)
                return (int)l;
            return value;
        }

        /// <summary>
        /// This JSON converter does not support writing.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => typeof(Inputs).IsAssignableFrom(objectType);
    }
}

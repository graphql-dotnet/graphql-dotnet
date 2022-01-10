using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Transports.Json;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="WebSocketMessage"/> object.
    /// </summary>
    public class WebSocketMessageJsonConverter : JsonConverter<WebSocketMessage>
    {
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, WebSocketMessage value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(WebSocketMessage.TYPE_KEY);
            writer.WriteStringValue(value.Type);
            if (value.Id != null)
            {
                writer.WritePropertyName(WebSocketMessage.ID_KEY);
                writer.WriteStringValue(value.Id);
            }
            if (value.Payload != null)
            {
                writer.WritePropertyName(WebSocketMessage.PAYLOAD_KEY);
                JsonSerializer.Serialize(writer, value.Payload, options);
            }
            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override WebSocketMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var request = new WebSocketMessage();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return request;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string key = reader.GetString();

                //unexpected end of data
                if (!reader.Read())
                    throw new JsonException();

                switch (key)
                {
                    case WebSocketMessage.TYPE_KEY:
                        request.Type = reader.GetString();
                        break;
                    case WebSocketMessage.ID_KEY:
                        request.Id = reader.GetString();
                        break;
                    case WebSocketMessage.PAYLOAD_KEY:
                        request.Payload = JsonSerializer.Deserialize<JsonElement?>(ref reader, options);
                        break;
                    default:
                        //unrecognized key
                        throw new JsonException();
                }
            }

            //unexpected end of data
            throw new JsonException();
        }
    }
}

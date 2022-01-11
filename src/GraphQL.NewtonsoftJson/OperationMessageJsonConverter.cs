using System;
using GraphQL.Transport;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="OperationMessage"/> object.
    /// </summary>
    public class OperationMessageJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(OperationMessage);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var request = (OperationMessage)value;
            writer.WriteStartObject();
            writer.WritePropertyName(OperationMessage.TYPE_KEY);
            writer.WriteValue(request.Type);
            if (request.Id != null)
            {
                writer.WritePropertyName(OperationMessage.ID_KEY);
                writer.WriteValue(request.Id);
            }
            if (request.Payload != null)
            {
                writer.WritePropertyName(OperationMessage.PAYLOAD_KEY);
                serializer.Serialize(writer, request.Payload);
            }
            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException();

            var request = new OperationMessage();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    return request;

                if (reader.TokenType != JsonToken.PropertyName)
                    throw new JsonException();

                string key = (string)reader.Value;

                switch (key)
                {
                    case OperationMessage.TYPE_KEY:
                        request.Type = reader.ReadAsString();
                        break;
                    case OperationMessage.ID_KEY:
                        request.Id = reader.ReadAsString();
                        break;
                    case OperationMessage.PAYLOAD_KEY:
                        if (!reader.Read())
                            throw new JsonException();
                        request.Payload = serializer.Deserialize<object>(reader);
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

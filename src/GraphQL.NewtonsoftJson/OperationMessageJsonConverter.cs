using GraphQL.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="OperationMessage"/> object.
    /// </summary>
    public class OperationMessageJsonConverter : JsonConverter
    {
        private const string TYPE_KEY = "type";
        private const string ID_KEY = "id";
        private const string PAYLOAD_KEY = "payload";

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(OperationMessage);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var request = (OperationMessage)value;
            writer.WriteStartObject();
            if (request.Type != null)
            {
                writer.WritePropertyName(TYPE_KEY);
                writer.WriteValue(request.Type);
            }
            if (request.Id != null)
            {
                writer.WritePropertyName(ID_KEY);
                writer.WriteValue(request.Id);
            }
            if (request.Payload != null)
            {
                writer.WritePropertyName(PAYLOAD_KEY);
                serializer.Serialize(writer, request.Payload);
            }
            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
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

                string key = (string)reader.Value!;

                switch (key)
                {
                    case TYPE_KEY:
                        request.Type = reader.ReadAsString();
                        break;
                    case ID_KEY:
                        request.Id = reader.ReadAsString();
                        break;
                    case PAYLOAD_KEY:
                        if (!reader.Read())
                            throw new JsonException();
                        request.Payload = serializer.Deserialize<JObject>(reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            //unexpected end of data
            throw new JsonException();
        }
    }
}

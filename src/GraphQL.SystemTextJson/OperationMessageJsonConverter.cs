using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Transport;

namespace GraphQL.SystemTextJson;

/// <summary>
/// A custom JsonConverter for reading or writing a <see cref="OperationMessage"/> object.
/// </summary>
public class OperationMessageJsonConverter : JsonConverter<OperationMessage>
{
    private const string TYPE_KEY = "type";
    private const string ID_KEY = "id";
    private const string PAYLOAD_KEY = "payload";

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, OperationMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Type != null)
        {
            writer.WritePropertyName(TYPE_KEY);
            writer.WriteStringValue(value.Type);
        }
        if (value.Id != null)
        {
            writer.WritePropertyName(ID_KEY);
            writer.WriteStringValue(value.Id);
        }
        if (value.Payload != null)
        {
            writer.WritePropertyName(PAYLOAD_KEY);
            JsonSerializer.Serialize(writer, value.Payload, options);
        }
        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public override OperationMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var request = new OperationMessage();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return request;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string key = reader.GetString()!;

            //unexpected end of data
            if (!reader.Read())
                throw new JsonException();

            switch (key)
            {
                case TYPE_KEY:
                    request.Type = reader.GetString();
                    break;
                case ID_KEY:
                    request.Id = reader.GetString();
                    break;
                case PAYLOAD_KEY:
                    request.Payload = JsonSerializer.Deserialize<JsonElement?>(ref reader, options);
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

using GraphQL.Transport;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson;

/// <summary>
/// A custom JsonConverter for reading or writing a <see cref="GraphQLRequest"/> object.
/// </summary>
public class GraphQLRequestJsonConverter : JsonConverter
{
    /// <summary>
    /// Name for the operation name parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string OPERATION_NAME_KEY = "operationName";

    /// <summary>
    /// Name for the query parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string QUERY_KEY = "query";

    /// <summary>
    /// Name for the variables parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string VARIABLES_KEY = "variables";

    /// <summary>
    /// Name for the extensions parameter.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
    /// </summary>
    private const string EXTENSIONS_KEY = "extensions";

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(GraphQLRequest);

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var request = (GraphQLRequest)value!;
        writer.WriteStartObject();
        if (request.Query != null)
        {
            writer.WritePropertyName(QUERY_KEY);
            writer.WriteValue(request.Query);
        }
        if (request.OperationName != null)
        {
            writer.WritePropertyName(OPERATION_NAME_KEY);
            writer.WriteValue(request.OperationName);
        }
        if (request.Variables != null)
        {
            writer.WritePropertyName(VARIABLES_KEY);
            serializer.Serialize(writer, request.Variables);
        }
        if (request.Extensions != null)
        {
            writer.WritePropertyName(EXTENSIONS_KEY);
            serializer.Serialize(writer, request.Extensions);
        }
        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonException();

        var request = new GraphQLRequest();

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
                return request;

            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonException();

            string key = (string)reader.Value!;

            switch (key)
            {
                case QUERY_KEY:
                    request.Query = reader.ReadAsString()!;
                    break;
                case OPERATION_NAME_KEY:
                    request.OperationName = reader.ReadAsString();
                    break;
                case VARIABLES_KEY:
                    if (!reader.Read())
                        throw new JsonException();
                    request.Variables = serializer.Deserialize<Inputs>(reader);
                    break;
                case EXTENSIONS_KEY:
                    if (!reader.Read())
                        throw new JsonException();
                    request.Extensions = serializer.Deserialize<Inputs>(reader);
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

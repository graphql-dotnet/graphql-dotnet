using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Transports.Json;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="GraphQLRequest"/> object.
    /// </summary>
    public class GraphQLRequestConverter : JsonConverter<GraphQLRequest>
    {
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, GraphQLRequest value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(GraphQLRequest.QUERY_KEY);
            writer.WriteStringValue(value.Query);
            if (value.OperationName != null)
            {
                writer.WritePropertyName(GraphQLRequest.OPERATION_NAME_KEY);
                writer.WriteStringValue(value.OperationName);
            }
            if (value.Variables != null)
            {
                writer.WritePropertyName(GraphQLRequest.VARIABLES_KEY);
                JsonSerializer.Serialize(writer, value.Variables, options);
            }
            if (value.Extensions != null)
            {
                writer.WritePropertyName(GraphQLRequest.EXTENSIONS_KEY);
                JsonSerializer.Serialize(writer, value.Extensions, options);
            }
            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override GraphQLRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var request = new GraphQLRequest();

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
                    case GraphQLRequest.QUERY_KEY:
                        request.Query = reader.GetString();
                        break;
                    case GraphQLRequest.OPERATION_NAME_KEY:
                        request.OperationName = reader.GetString();
                        break;
                    case GraphQLRequest.VARIABLES_KEY:
                        request.Variables = JsonSerializer.Deserialize<Inputs>(ref reader, options);
                        break;
                    case GraphQLRequest.EXTENSIONS_KEY:
                        request.Extensions = JsonSerializer.Deserialize<Inputs>(ref reader, options);
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

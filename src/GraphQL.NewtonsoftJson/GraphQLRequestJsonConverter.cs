using System;
using GraphQL.Transports.Json;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="GraphQLRequest"/> object.
    /// </summary>
    public class GraphQLRequestJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(GraphQLRequest);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var request = (GraphQLRequest)value;
            writer.WriteStartObject();
            writer.WritePropertyName(GraphQLRequest.QUERY_KEY);
            writer.WriteValue(request.Query);
            if (request.OperationName != null)
            {
                writer.WritePropertyName(GraphQLRequest.OPERATION_NAME_KEY);
                writer.WriteValue(request.OperationName);
            }
            if (request.Variables != null)
            {
                writer.WritePropertyName(GraphQLRequest.VARIABLES_KEY);
                serializer.Serialize(writer, request.Variables);
            }
            if (request.Extensions != null)
            {
                writer.WritePropertyName(GraphQLRequest.EXTENSIONS_KEY);
                serializer.Serialize(writer, request.Extensions);
            }
            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException();

            var request = new GraphQLRequest();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    return request;

                if (reader.TokenType != JsonToken.PropertyName)
                    throw new JsonException();

                string key = (string)reader.Value;

                switch (key)
                {
                    case GraphQLRequest.QUERY_KEY:
                        request.Query = reader.ReadAsString();
                        break;
                    case GraphQLRequest.OPERATION_NAME_KEY:
                        request.OperationName = reader.ReadAsString();
                        break;
                    case GraphQLRequest.VARIABLES_KEY:
                        if (!reader.Read())
                            throw new JsonException();
                        request.Variables = serializer.Deserialize<Inputs>(reader);
                        break;
                    case GraphQLRequest.EXTENSIONS_KEY:
                        if (!reader.Read())
                            throw new JsonException();
                        request.Extensions = serializer.Deserialize<Inputs>(reader);
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

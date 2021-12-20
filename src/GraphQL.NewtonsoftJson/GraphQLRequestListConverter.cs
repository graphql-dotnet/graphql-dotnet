using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// A custom JsonConverter for reading or writing a <see cref="GraphQLRequest"/> object.
    /// </summary>
    public class GraphQLRequestListConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return (
                objectType == typeof(IEnumerable<GraphQLRequest>) ||
                objectType == typeof(ICollection<GraphQLRequest>) ||
                objectType == typeof(IReadOnlyCollection<GraphQLRequest>) ||
                objectType == typeof(IReadOnlyList<GraphQLRequest>) ||
                objectType == typeof(IList<GraphQLRequest>) ||
                objectType == typeof(List<GraphQLRequest>) ||
                objectType == typeof(GraphQLRequest[]));
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var requests = (IEnumerable<GraphQLRequest>)value;
            writer.WriteStartArray();
            foreach (var request in requests)
            {
                serializer.Serialize(writer, request);
            }
            writer.WriteEndArray();
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var request = serializer.Deserialize<GraphQLRequest>(reader);
                return objectType == typeof(List<GraphQLRequest>)
                    ? new List<GraphQLRequest>(1) { request }
                    : new GraphQLRequest[] { request };
            }

            //unexpected token type
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonException();

            var list = new List<GraphQLRequest>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    return objectType == typeof(GraphQLRequest[])
                        ? list.ToArray()
                        : list;
                }

                var request = serializer.Deserialize<GraphQLRequest>(reader);
                list.Add(request);
            }

            //unexpected end of data
            throw new JsonException();
        }
    }
}

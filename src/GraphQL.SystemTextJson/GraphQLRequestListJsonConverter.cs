using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Transport;

namespace GraphQL.SystemTextJson;

/// <summary>
/// A custom JsonConverter for reading or writing a list of <see cref="GraphQLRequest"/> objects.
/// Will deserialize a single request into a list containing one request.
/// <br/><br/>
/// To determine if a single request is a batch request or not, deserialize to the type
/// <see cref="IList{T}">IList</see>&lt;<see cref="GraphQLRequest"/>&gt; and examine the type
/// of the returned object to see if it is <see cref="GraphQLRequest"/>[].
/// If the returned object is an array, then it is not a batch request.
/// </summary>
public class GraphQLRequestListJsonConverter : JsonConverter<IEnumerable<GraphQLRequest>>
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return (
            typeToConvert == typeof(IList<GraphQLRequest>) ||
            typeToConvert == typeof(GraphQLRequest[]) ||
            typeToConvert == typeof(List<GraphQLRequest>) ||
            typeToConvert == typeof(IEnumerable<GraphQLRequest>) ||
            typeToConvert == typeof(ICollection<GraphQLRequest>) ||
            typeToConvert == typeof(IReadOnlyCollection<GraphQLRequest>) ||
            typeToConvert == typeof(IReadOnlyList<GraphQLRequest>));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IEnumerable<GraphQLRequest> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var request in value)
        {
            JsonSerializer.Serialize(writer, request, options);
        }
        writer.WriteEndArray();
    }

    /// <inheritdoc/>
    public override IEnumerable<GraphQLRequest> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var request = JsonSerializer.Deserialize<GraphQLRequest>(ref reader, options)!;
            // do not change behavior here; see class notes
            return typeToConvert == typeof(List<GraphQLRequest>)
                ? new List<GraphQLRequest>(1) { request }
                : new GraphQLRequest[] { request };
        }

        //unexpected token type
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var list = new List<GraphQLRequest>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return typeToConvert == typeof(GraphQLRequest[])
                    ? list.ToArray()
                    : list;
            }

            var request = JsonSerializer.Deserialize<GraphQLRequest>(ref reader, options)!;
            list.Add(request);
        }

        //unexpected end of data
        throw new JsonException();
    }
}

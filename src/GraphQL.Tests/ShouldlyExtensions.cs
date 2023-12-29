#nullable enable

using System.Text.Json;
using GraphQL.SystemTextJson;

namespace GraphQL.Tests;

public static class ShouldlyExtensions
{
    private static readonly IGraphQLTextSerializer _serializer = new GraphQLSerializer();

    /// <summary>
    /// Compares the objects as if they were both serialized to JSON.
    /// You may pass a JSON string to <paramref name="expected"/> or <paramref name="actual"/>
    /// which will normalize the JSON prior to comparison. The serializer used is
    /// <see cref="GraphQLSerializer"/> and is configured to properly serialize
    /// <see cref="ExecutionResult"/> objects.
    /// </summary>
    public static void ShouldBeSimilarTo(this object? actual, object? expected, string? customMessage = null)
    {
        if (actual is string str)
            actual = _serializer.Deserialize<JsonElement>(str);
        if (expected is string str2)
            expected = _serializer.Deserialize<JsonElement>(str2);
        string expectedJson = _serializer.Serialize(expected);
        string actualJson = _serializer.Serialize(actual);
        actualJson.ShouldBe(expectedJson, customMessage);
    }
}

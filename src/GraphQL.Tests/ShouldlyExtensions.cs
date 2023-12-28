#nullable enable

using GraphQL.SystemTextJson;

namespace GraphQL.Tests;

public static class ShouldlyExtensions
{
    private static readonly IGraphQLTextSerializer _serializer = new GraphQLSerializer();

    public static void ShouldBeSimilarTo(this object? actual, object? expected, string? customMessage = null)
    {
        if (expected is string str)
            expected = _serializer.Deserialize<Inputs>(str);
        string expectedJson = _serializer.Serialize(expected);
        string actualJson = _serializer.Serialize(actual);
        actualJson.ShouldBe(expectedJson, customMessage);
    }
}

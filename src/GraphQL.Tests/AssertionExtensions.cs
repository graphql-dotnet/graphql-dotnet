using System.Text.Json;
using Shouldly;

namespace GraphQL.Tests
{
    public static class AssertionExtensions
    {
        public static void ShouldBeCrossPlat(this string actual, string expected, string customMessage)
            => Normalize(actual).ShouldBe(Normalize(expected), customMessage);

        public static void ShouldBeCrossPlat(this string actual, string expected)
            => Normalize(actual).ShouldBe(Normalize(expected));

        public static void ShouldBeCrossPlatJson(this string actualJson, string expectedJson)
        {
            using var actualJsonDoc = JsonDocument.Parse(Normalize(actualJson));
            using var expectedJsonDoc = JsonDocument.Parse(Normalize(expectedJson));
            JsonSerializer.Serialize(actualJsonDoc.RootElement).ShouldBe(JsonSerializer.Serialize(expectedJsonDoc.RootElement));
        }

        private static string Normalize(this string value) => value?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
    }
}

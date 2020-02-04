using Shouldly;

namespace GraphQL.Tests
{
    public static class AssertionExtensions
    {
        public static void ShouldBeCrossPlat(this string actual, string expected, string customMessage)
        {
            var actualNormalized = actual?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            var expectedNormalized = expected?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            actualNormalized.ShouldBe(expectedNormalized, customMessage);
        }

        public static void ShouldBeCrossPlat(this string actual, string expected)
        {
            var actualNormalized = actual?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            var expectedNormalized = expected?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            actualNormalized.ShouldBe(expectedNormalized);
        }
    }
}

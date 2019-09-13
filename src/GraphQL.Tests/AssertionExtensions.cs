using Shouldly;

namespace GraphQL.Tests
{
    public static class AssertionExtensions
    {
        public static void ShouldBeCrossPlat(this string a, string b, string customMessage)
        {
            var aa = a?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            var bb = b?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            aa.ShouldBe(bb, customMessage);
        }

        public static void ShouldBeCrossPlat(this string a, string b)
        {
            var aa = a?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            var bb = b?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
            aa.ShouldBe(bb);
        }
    }
}

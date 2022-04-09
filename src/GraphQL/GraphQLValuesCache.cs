using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL
{
    internal static class GraphQLValuesCache
    {
        private static readonly object[] _positiveInts = new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        private static readonly object[] _negativeInts = new object[] { 0, -1, -2, -3, -4, -5, -6, -7, -8, -9 };
        private static readonly object[] _positiveLongs = new object[] { 0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L };
        private static readonly object[] _negativeLongs = new object[] { 0L, -1L, -2L, -3L, -4L, -5L, -6L, -7L, -8L, -9L };

        public static object GetInt(ROM value)
        {
            return value.Length switch
            {
                1 when '0' <= value.Span[0] && value.Span[0] <= '9' => _positiveInts[value.Span[0] - '0'],
                2 when value.Span[0] == '-' && '0' <= value.Span[1] && value.Span[1] <= '9' => _negativeInts[value.Span[1] - '0'],
                _ => Int.Parse(value)
            };
        }

        public static object GetLong(ROM value)
        {
            return value.Length switch
            {
                1 when '0' <= value.Span[0] && value.Span[0] <= '9' => _positiveLongs[value.Span[0] - '0'],
                2 when value.Span[0] == '-' && '0' <= value.Span[1] && value.Span[1] <= '9' => _negativeLongs[value.Span[1] - '0'],
                _ => Long.Parse(value)
            };
        }

        public static readonly GraphQLNullValue Null = new();

        public static readonly GraphQLBooleanValue True = new GraphQLTrueBooleanValue();

        public static readonly GraphQLBooleanValue False = new GraphQLFalseBooleanValue();
    }
}

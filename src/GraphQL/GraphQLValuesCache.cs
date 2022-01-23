using System.Collections.Generic;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL
{
    internal class GraphQLValuesCache
    {
        private static readonly Dictionary<ROM, object> _ints = new Dictionary<ROM, object>
        {
            ["-10"] = -10,
            ["-9"] = -9,
            ["-8"] = -8,
            ["-7"] = -7,
            ["-6"] = -6,
            ["-5"] = -5,
            ["-4"] = -4,
            ["-3"] = -3,
            ["-2"] = -2,
            ["-1"] = -1,
            ["0"] = 0,
            ["1"] = 1,
            ["2"] = 2,
            ["3"] = 3,
            ["4"] = 4,
            ["5"] = 5,
            ["6"] = 6,
            ["7"] = 7,
            ["8"] = 8,
            ["9"] = 9,
            ["10"] = 10,
        };

        private static readonly Dictionary<ROM, object> _longs = new Dictionary<ROM, object>
        {
            ["-10"] = -10L,
            ["-9"] = -9L,
            ["-8"] = -8L,
            ["-7"] = -7L,
            ["-6"] = -6L,
            ["-5"] = -5L,
            ["-4"] = -4L,
            ["-3"] = -3L,
            ["-2"] = -2L,
            ["-1"] = -1L,
            ["0"] = 0L,
            ["1"] = 1L,
            ["2"] = 2L,
            ["3"] = 3L,
            ["4"] = 4L,
            ["5"] = 5L,
            ["6"] = 6L,
            ["7"] = 7L,
            ["8"] = 8L,
            ["9"] = 9L,
            ["10"] = 10L,
        };

        public static object GetInt(ROM value) => _ints.TryGetValue(value, out object i) ? i : Int.Parse(value);

        public static object GetLong(ROM value) => _longs.TryGetValue(value, out object i) ? i : Long.Parse(value);

        public static readonly GraphQLNullValue Null = new GraphQLNullValue();

        public static readonly GraphQLBooleanValue True = new GraphQLTrueBooleanValue();

        public static readonly GraphQLBooleanValue False = new GraphQLFalseBooleanValue();
    }
}

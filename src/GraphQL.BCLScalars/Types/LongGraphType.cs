using System.Numerics;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Long scalar graph type represents a signed 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="long"/> .NET values to this scalar graph type.
    /// </summary>
    public class LongGraphType : ScalarGraphType
    {
        private static readonly object[] _positiveLongs = new object[] { 0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L };
        private static readonly object[] _negativeLongs = new object[] { 0L, -1L, -2L, -3L, -4L, -5L, -6L, -7L, -8L, -9L };

        internal static object GetLong(ROM value)
        {
            return value.Length switch
            {
                1 when '0' <= value.Span[0] && value.Span[0] <= '9' => _positiveLongs[value.Span[0] - '0'],
                2 when value.Span[0] == '-' && '0' <= value.Span[1] && value.Span[1] <= '9' => _negativeLongs[value.Span[1] - '0'],
                _ => Long.Parse(value)
            };
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => GetLong(x.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => Long.TryParse(x.Value, out var _),
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            long _ => value,
            null => null,
            int i => checked((long)i),
            sbyte sb => checked((long)sb),
            byte b => checked((long)b),
            short s => checked((long)s),
            ushort us => checked((long)us),
            uint ui => checked((long)ui),
            ulong ul => checked((long)ul),
            BigInteger bi => checked((long)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}

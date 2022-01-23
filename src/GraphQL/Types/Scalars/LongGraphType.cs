using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Long scalar graph type represents a signed 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="long"/> .NET values to this scalar graph type.
    /// </summary>
    public class LongGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => GraphQLValuesCache.GetLong(x.Value),
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

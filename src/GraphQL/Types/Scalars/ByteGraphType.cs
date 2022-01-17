using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Byte scalar graph type represents an unsigned 8-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="byte"/> .NET values to this scalar graph type.
    /// </summary>
    public class ByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => Byte.Parse(x.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => Byte.TryParse(x.Value, out var _),
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            byte _ => value,
            null => null,
            int i => checked((byte)i),
            sbyte sb => checked((byte)sb),
            short s => checked((byte)s),
            ushort us => checked((byte)us),
            uint ui => checked((byte)ui),
            long l => checked((byte)l),
            ulong ul => checked((byte)ul),
            BigInteger bi => checked((byte)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}

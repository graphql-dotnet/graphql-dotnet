using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The ULong scalar graph type represents an unsigned 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ulong"/> .NET values to this scalar graph type.
    /// </summary>
    public class ULongGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => ULong.Parse(x.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => ULong.TryParse(x.Value, out var _),
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            ulong _ => value,
            null => null,
            int i => checked((ulong)i),
            long l => checked((ulong)l),
            BigInteger bi => checked((ulong)bi),
            sbyte sb => checked((ulong)sb),
            byte b => checked((ulong)b),
            short s => checked((ulong)s),
            ushort us => checked((ulong)us),
            uint ui => checked((ulong)ui),
            _ => ThrowValueConversionError(value)
        };
    }
}

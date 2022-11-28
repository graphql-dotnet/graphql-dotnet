using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The UShort scalar graph type represents an unsigned 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ushort"/> .NET values to this scalar graph type.
    /// </summary>
    public class UShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => UShort.Parse(x.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => UShort.TryParse(x.Value, out var _),
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            ushort _ => value,
            null => null,
            int i => checked((ushort)i),
            sbyte sb => checked((ushort)sb),
            byte b => checked((ushort)b),
            short s => checked((ushort)s),
            uint ui => checked((ushort)ui),
            long l => checked((ushort)l),
            ulong ul => checked((ushort)ul),
            BigInteger bi => checked((ushort)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}

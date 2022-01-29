using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Short scalar graph type represents a signed 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="short"/> .NET values to this scalar graph type.
    /// </summary>
    public class ShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => Short.Parse(x.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => Short.TryParse(x.Value, out var _),
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            short _ => value,
            null => null,
            int i => checked((short)i),
            sbyte sb => checked((short)sb),
            byte b => checked((short)b),
            ushort us => checked((short)us),
            uint ui => checked((short)ui),
            long l => checked((short)l),
            ulong ul => checked((short)ul),
            BigInteger bi => checked((short)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}

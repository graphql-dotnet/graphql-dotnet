using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The SByte scalar graph type represents a signed 8-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="sbyte"/> .NET values to this scalar graph type.
    /// </summary>
    public class SByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => SByte.Parse(x.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => SByte.TryParse(x.Value, out var _),
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            sbyte _ => value,
            null => null,
            int i => checked((sbyte)i),
            byte b => checked((sbyte)b),
            short s => checked((sbyte)s),
            ushort us => checked((sbyte)us),
            uint ui => checked((sbyte)ui),
            long l => checked((sbyte)l),
            ulong ul => checked((sbyte)ul),
            BigInteger bi => checked((sbyte)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}

using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Byte scalar graph type represents an unsigned 8-bit integer value.
    /// </summary>
    public class ByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            ByteValue byteValue => byteValue.Value,
            IntValue intValue => byte.MinValue <= intValue.Value && intValue.Value <= byte.MaxValue ? (byte?)intValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(byte));
    }
}

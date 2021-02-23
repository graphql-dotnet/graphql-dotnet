using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The SByte scalar graph type represents a signed 8-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="sbyte"/> .NET values to this scalar graph type.
    /// </summary>
    public class SByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            SByteValue sbyteValue => sbyteValue.Value,
            IntValue intValue => sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue ? (sbyte?)intValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(sbyte));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            SByteValue _ => true,
            IntValue intValue => sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue,
            _ => false
        };
    }
}

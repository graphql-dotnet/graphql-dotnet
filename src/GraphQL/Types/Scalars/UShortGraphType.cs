using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The UShort scalar graph type represents an unsigned 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ushort"/> .NET values to this scalar graph type.
    /// </summary>
    public class UShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            UShortValue ushortValue => ushortValue.Value,
            IntValue intValue => ushort.MinValue <= intValue.Value && intValue.Value <= ushort.MaxValue ? (ushort?)intValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ushort));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            UShortValue _ => true,
            IntValue intValue => ushort.MinValue <= intValue.Value && intValue.Value <= ushort.MaxValue,
            _ => false
        };
    }
}

using System;
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
            IntValue intValue => ushort.MinValue <= intValue.Value && intValue.Value <= ushort.MaxValue ? (ushort?)intValue.Value : null,
            LongValue longValue => ushort.MinValue <= longValue.Value && longValue.Value <= ushort.MaxValue ? (ushort?)longValue.Value : null,
            BigIntValue bigIntValue => ushort.MinValue <= bigIntValue.Value && bigIntValue.Value <= ushort.MaxValue ? (ushort?)bigIntValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ushort));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => ushort.MinValue <= intValue.Value && intValue.Value <= ushort.MaxValue,
            LongValue longValue => ushort.MinValue <= longValue.Value && longValue.Value <= ushort.MaxValue,
            BigIntValue bigIntValue => ushort.MinValue <= bigIntValue.Value && bigIntValue.Value <= ushort.MaxValue,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new IntValue(Convert.ToUInt16(value));
    }
}

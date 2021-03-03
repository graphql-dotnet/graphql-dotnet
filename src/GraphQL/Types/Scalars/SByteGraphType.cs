using System;
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
            IntValue intValue => sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue ? (sbyte?)intValue.Value : null,
            LongValue longValue => sbyte.MinValue <= longValue.Value && longValue.Value <= sbyte.MaxValue ? (sbyte?)longValue.Value : null,
            BigIntValue bigIntValue => sbyte.MinValue <= bigIntValue.Value && bigIntValue.Value <= sbyte.MaxValue ? (sbyte?)bigIntValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(sbyte));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue,
            LongValue longValue => sbyte.MinValue <= longValue.Value && longValue.Value <= sbyte.MaxValue,
            BigIntValue bigIntValue => sbyte.MinValue <= bigIntValue.Value && bigIntValue.Value <= sbyte.MaxValue,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new IntValue(Convert.ToSByte(value));
    }
}

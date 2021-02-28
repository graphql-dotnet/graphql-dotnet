using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The ULong scalar graph type represents an unsigned 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ulong"/> .NET values to this scalar graph type.
    /// </summary>
    public class ULongGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => intValue.Value >= 0 ? (ulong?)intValue.Value : null,
            LongValue longValue => longValue.Value >= 0 ? (ulong?)longValue.Value : null,
            BigIntValue bigIntValue => bigIntValue.Value >= 0 && bigIntValue.Value <= ulong.MaxValue ? (ulong?)bigIntValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ulong));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => intValue.Value >= 0,
            LongValue longValue => longValue.Value >= 0,
            BigIntValue bigIntValue => bigIntValue.Value >= 0 && bigIntValue.Value <= ulong.MaxValue,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => value switch
        {
            ulong ulongValue => new BigIntValue(ulongValue),
            _ => new LongValue(Convert.ToInt64(value))
        };
    }
}

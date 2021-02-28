using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Long scalar graph type represents a signed 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="long"/> .NET values to this scalar graph type.
    /// </summary>
    public class LongGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => (long)intValue.Value,
            LongValue longValue => longValue.Value,
            BigIntValue bigIntValue => long.MinValue <= bigIntValue.Value && bigIntValue.Value <= long.MaxValue ? (long?)bigIntValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            long _ => value, // no boxing
            int i => (long)i,
            _ => null
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue _ => true,
            LongValue _ => true,
            BigIntValue bigIntValue => long.MinValue <= bigIntValue.Value && bigIntValue.Value <= long.MaxValue,
            _ => false
        };

        /// <inheritdoc/>
        public override bool CanParseValue(object value)
        {
            return value switch
            {
                long _ => true,
                int _ => true,
                _ => false
            };
        }

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new LongValue(Convert.ToInt64(value));
    }
}

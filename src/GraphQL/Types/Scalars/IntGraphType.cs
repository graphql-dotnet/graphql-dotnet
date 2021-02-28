using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="int"/> .NET values to this scalar graph type.
    /// </summary>
    public class IntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => intValue.Value,
            LongValue longValue => int.MinValue <= longValue.Value && longValue.Value <= int.MaxValue ? (int?)longValue.Value : null,
            BigIntValue bigIntValue => int.MinValue <= bigIntValue.Value && bigIntValue.Value <= int.MaxValue ? (int?)bigIntValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            int _ => value, // no boxing
            _ => null
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue _ => true,
            LongValue longValue => int.MinValue <= longValue.Value && longValue.Value <= int.MaxValue,
            BigIntValue bigIntValue => int.MinValue <= bigIntValue.Value && bigIntValue.Value <= int.MaxValue,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new IntValue(Convert.ToInt32(value));

        /// <inheritdoc/>
        public override bool CanParseValue(object value)
        {
            return value switch
            {
                int _ => true,
                _ => false
            };
        }
    }
}

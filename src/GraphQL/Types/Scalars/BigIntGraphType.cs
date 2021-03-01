using System;
using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The BigInt scalar graph type represents a signed integer with any number of digits.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="BigInteger"/> .NET values to this scalar graph type.
    /// </summary>
    public class BigIntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => new BigInteger(intValue.Value),
            LongValue longValue => new BigInteger(longValue.Value),
            BigIntValue bigIntValue => bigIntValue.Value,
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value == null ? null : ValueConverter.ConvertTo(value, typeof(BigInteger));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue _ => true,
            LongValue _ => true,
            BigIntValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => value switch
        {
            null => new NullValue(),
            _ => new BigIntValue((BigInteger)ValueConverter.ConvertTo(value, typeof(BigInteger)))
        };
    }
}

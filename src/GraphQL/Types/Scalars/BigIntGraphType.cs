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
            BigIntValue bigIntValue => bigIntValue.Value,
            LongValue longValue => new BigInteger(longValue.Value),
            IntValue intValue => new BigInteger(intValue.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(BigInteger));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            BigIntValue _ => true,
            LongValue _ => true,
            IntValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAst(object value) => new BigIntValue((BigInteger)ValueConverter.ConvertTo(value, typeof(BigInteger)));
    }
}

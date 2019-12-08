using GraphQL.Language.AST;
using System.Numerics;

namespace GraphQL.Types
{
    public class BigIntGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            BigIntValue bigIntValue => bigIntValue.Value,
            LongValue longValue => new BigInteger(longValue.Value),
            IntValue intValue => new BigInteger(intValue.Value),
            _ => (object)null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(BigInteger));

        public override object Serialize(object value) => ParseValue(value);
    }
}

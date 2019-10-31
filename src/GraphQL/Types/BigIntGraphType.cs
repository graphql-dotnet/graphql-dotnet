using GraphQL.Language.AST;
using System.Numerics;

namespace GraphQL.Types
{
    public class BigIntGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case BigIntValue bigIntValue:
                    return bigIntValue.Value;

                case LongValue longValue:
                    return new BigInteger(longValue.Value);

                case IntValue intValue:
                    return new BigInteger(intValue.Value);

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(BigInteger));

        public override object Serialize(object value) => ParseValue(value);
    }
}

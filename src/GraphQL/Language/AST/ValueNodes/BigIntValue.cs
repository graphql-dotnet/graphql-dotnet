using System.Numerics;

namespace GraphQL.Language.AST
{
    public class BigIntValue : ValueNode<BigInteger>
    {
        public BigIntValue(BigInteger value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<BigInteger> other) => Value == other.Value;
    }
}

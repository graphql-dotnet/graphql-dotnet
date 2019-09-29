using System.Numerics;

namespace GraphQL.Language.AST
{
    public class BigIntegerValue : ValueNode<BigInteger>
    {
        public BigIntegerValue(BigInteger value) => Value = value;

        protected override bool Equals(ValueNode<BigInteger> other) => Value == other.Value;
    }
}

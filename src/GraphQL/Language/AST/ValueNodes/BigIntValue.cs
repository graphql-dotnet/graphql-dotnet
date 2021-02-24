using System.Numerics;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="BigInteger"/> value within a document.
    /// </summary>
    public class BigIntValue : ValueNode<BigInteger>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public BigIntValue(BigInteger value) : base(value)
        {
        }
    }
}

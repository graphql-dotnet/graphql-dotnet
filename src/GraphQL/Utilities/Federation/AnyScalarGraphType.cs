using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// Represents a type unknown within this portion of the federated schema.
    /// </summary>
    public class AnyScalarGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public AnyScalarGraphType()
        {
            Name = "_Any";
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value.Value;

        /// <inheritdoc/>
        public override object ParseValue(object value) => value;

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => true;

        /// <inheritdoc/>
        public override bool CanParseValue(object value) => true;

        /// <inheritdoc/>
        public override bool IsValidDefault(object value) => true;

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new AnyValue(value);
    }
}

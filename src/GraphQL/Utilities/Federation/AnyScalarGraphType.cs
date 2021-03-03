using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    public class AnyScalarGraphType : ScalarGraphType
    {
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

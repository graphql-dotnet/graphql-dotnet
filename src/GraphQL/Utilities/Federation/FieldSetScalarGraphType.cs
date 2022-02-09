using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    public class FieldSetScalarGraphType : ScalarGraphType
    {
        public FieldSetScalarGraphType()
        {
            Name = "_FieldSet";
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value.ParseAnyLiteral();

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value;

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => true;

        /// <inheritdoc/>
        public override bool CanParseValue(object? value) => true;

        /// <inheritdoc/>
        public override bool IsValidDefault(object value) => true;

        /// <inheritdoc/>
        public override GraphQLValue ToAST(object? value) => ThrowASTConversionError(value);
    }
}

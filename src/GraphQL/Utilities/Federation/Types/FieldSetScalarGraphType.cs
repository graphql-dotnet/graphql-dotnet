#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// A custom scalar type that is used to represent a set of fields.Grammatically, a field set
    /// is a <see href="https://spec.graphql.org/October2021/#SelectionSet">selection set</see>
    /// minus the braces. This means it can represent a single field "upc", multiple fields
    /// "id countryCode", and even nested selection sets "id organization { id }".
    /// <br/>
    /// <see href="https://www.apollographql.com/docs/federation/federation-spec/#scalar-_fieldset"/>
    /// </summary>
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

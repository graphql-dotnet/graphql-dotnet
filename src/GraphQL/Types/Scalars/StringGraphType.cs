using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The String scalar graph type represents a string value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="string"/> .NET values to this scalar graph type.
    /// </summary>
    public class StringGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLStringValue s => (string)s.Value, //ISSUE:allocation
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value is GraphQLStringValue || value is GraphQLNullValue;

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            string _ => value,
            null => null,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseValue(object? value) => value is string || value == null;
    }
}

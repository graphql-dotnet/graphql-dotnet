using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Boolean scalar graph type represents a boolean value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="bool"/> .NET values to this scalar graph type.
    /// </summary>
    public class BooleanGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLBooleanValue b => b.BoolValue.Boxed(),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value)
            => value is GraphQLBooleanValue || value is GraphQLNullValue;

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            bool _ => value,
            null => null,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseValue(object? value) => value is bool || value == null;

        /// <inheritdoc/>
        public override GraphQLValue ToAST(object? value) => value switch
        {
            bool b => b ? GraphQLValuesCache.True : GraphQLValuesCache.False,
            null => GraphQLValuesCache.Null,
            _ => ThrowASTConversionError(value)
        };
    }
}

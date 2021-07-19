using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The String scalar graph type represents a string value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="string"/> .NET values to this scalar graph type.
    /// </summary>
    public class StringGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(IValue value) => value switch
        {
            StringValue s => s.Value,
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value is StringValue || value is NullValue;

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

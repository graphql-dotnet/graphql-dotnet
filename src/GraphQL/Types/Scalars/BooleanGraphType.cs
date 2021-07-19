using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Boolean scalar graph type represents a boolean value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="bool"/> .NET values to this scalar graph type.
    /// </summary>
    public class BooleanGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(IValue value) => value switch
        {
            BooleanValue b => b.Value.Boxed(),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value is BooleanValue || value is NullValue;

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
        public override IValue? ToAST(object? value) => value switch
        {
            bool b => new BooleanValue(b),
            null => new NullValue(),
            _ => ThrowASTConversionError(value)
        };
    }
}

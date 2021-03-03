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
        public override object ParseLiteral(IValue value) => ((value as BooleanValue)?.Value).Boxed();

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(bool));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value is BooleanValue;

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new BooleanValue((bool)value);
    }
}

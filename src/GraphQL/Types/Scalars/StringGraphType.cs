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
        public override object ParseLiteral(IValue value) => (value as StringValue)?.Value;

        /// <inheritdoc/>
        public override object ParseValue(object value) => value?.ToString();

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value is StringValue;

        /// <inheritdoc/>
        public override IValue ToAst(object value) => new StringValue(value.ToString());
    }
}

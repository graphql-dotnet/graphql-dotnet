using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Boolean scalar graph type represents a boolean value.
    /// </summary>
    public class BooleanGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(bool));

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => ((value as BooleanValue)?.Value).Boxed();
    }
}

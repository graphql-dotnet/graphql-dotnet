using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The String scalar graph type represents a string value. It is one of the five built-in scalars. 
    /// </summary>
    public class StringGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseValue(object value) => value?.ToString();

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => (value as StringValue)?.Value;
    }
}

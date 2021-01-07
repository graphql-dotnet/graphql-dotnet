using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The String scalar graph type represents a string value. It is one of the five built-in scalars.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="string"/> .NET values to this scalar graph type.
    /// </summary>
    public class StringGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => (value as StringValue)?.Value;

        /// <inheritdoc/>
        public override object ParseValue(object value) => value?.ToString();
    }
}

using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Short scalar graph type represents a signed 16-bit integer value.
    /// </summary>
    public class ShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            ShortValue shortValue => shortValue.Value,
            IntValue intValue => short.MinValue <= intValue.Value && intValue.Value <= short.MaxValue ? (short?)intValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(short));
    }
}

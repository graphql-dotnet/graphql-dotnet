using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The SByte scalar graph type represents a signed 8-bit integer value.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="sbyte"/> .NET values to this scalar graph type.
    /// </summary>
    public class SByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            SByteValue sbyteValue => sbyteValue.Value,
            IntValue intValue => sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue ? (sbyte?)intValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(sbyte));
    }
}

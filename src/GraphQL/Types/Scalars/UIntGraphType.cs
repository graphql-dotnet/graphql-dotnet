using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The ULong scalar graph type represents an unsigned 32-bit integer value.
    /// </summary>
    public class UIntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            UIntValue uintValue => uintValue.Value,
            IntValue intValue => intValue.Value >= 0 ? (uint?)intValue.Value : null,
            LongValue longValue => uint.MinValue <= longValue.Value && longValue.Value <= uint.MaxValue ? (uint?)longValue.Value : null,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(uint));
    }
}

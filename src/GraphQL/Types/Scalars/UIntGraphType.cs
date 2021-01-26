using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The UInt scalar graph type represents an unsigned 32-bit integer value.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="uint"/> .NET values to this scalar graph type.
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

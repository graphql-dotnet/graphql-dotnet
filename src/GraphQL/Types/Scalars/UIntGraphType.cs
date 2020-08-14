using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UIntGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            UIntValue uintValue => uintValue.Value,
            IntValue intValue => intValue.Value >= 0 ? (uint?)intValue.Value : null,
            LongValue longValue => uint.MinValue <= longValue.Value && longValue.Value <= uint.MaxValue ? (uint?)longValue.Value : null,
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(uint));
    }
}
